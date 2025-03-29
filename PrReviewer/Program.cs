// Program.cs
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

string openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY not found");
string head = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF") ?? throw new Exception("GITHUB_HEAD_REF not found");
string @base = Environment.GetEnvironmentVariable("GITHUB_BASE_REF") ?? throw new Exception("GITHUB_BASE_REF not found");
string githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new Exception("GITHUB_TOKEN not found");
string repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ?? throw new Exception("GITHUB_REPOSITORY not found");
string prNumber = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER") ?? throw new Exception("GITHUB_PR_NUMBER not found");

Console.WriteLine($"🔧 GITHUB_BASE_REF: {@base}");
Console.WriteLine($"🔧 GITHUB_HEAD_REF: {head}");
Console.WriteLine($"🔧 PR: #{prNumber} in repo: {repo}");

Console.WriteLine("🔍 Fetching changed files...");

var files = GetChangedFiles(@base, head);

if (files.Count == 0)
{
    Console.WriteLine("✅ No files changed. Nothing to analyze.");
    return;
}

Console.WriteLine($"📁 {files.Count} changed file(s): {string.Join(", ", files)}");

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiApiKey);

foreach (var file in files)
{
    Console.WriteLine($"📄 Processing {file}...");
    if (!File.Exists(file))
    {
        Console.WriteLine("⚠️  File not found, skip.");
        continue;
    }

    string fileContent = await File.ReadAllTextAsync(file);
    if (string.IsNullOrWhiteSpace(fileContent))
    {
        Console.WriteLine("⚠️  File is empty, skip.");
        continue;
    }

    string fileDiff = GetFileDiff(file, @base, head);
    if (string.IsNullOrWhiteSpace(fileDiff))
    {
        Console.WriteLine("⚠️  No diff found, skip.");
        continue;
    }

    var requestBody = new
    {
        model = "gpt-4",
        messages = new[]
        {
            new { role = "system", content = "你是一位資深工程助理，請根據提供的原始檔與變更進行評論建議。" },
            new { role = "user", content = $"原始檔案內容：\n{fileContent}\n\n改動內容：\n{fileDiff}" }
        },
        temperature = 0.7
    };

    Console.WriteLine("🤖 Calling OpenAI...");
    var response = await httpClient.PostAsync(
        "https://api.openai.com/v1/chat/completions",
        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(responseContent);
    var summary = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

    Console.WriteLine("🧠 GPT 回應：\n" + summary);

    Console.WriteLine("💬 Posting to PR...");
    var commentBody = new { body = $"🧠 GPT Review 建議 for `{file}`\n\n{summary}" };
    var commentReq = new StringContent(JsonSerializer.Serialize(commentBody), Encoding.UTF8, "application/json");
    // commentReq.Headers.Authorization = new AuthenticationHeaderValue("token", githubToken);

    var prApi = $"https://api.github.com/repos/{repo}/issues/{prNumber}/comments";
    var gh = new HttpClient();
    gh.DefaultRequestHeaders.UserAgent.ParseAdd("gpt-pr-reviewer-dotnet");
    gh.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", githubToken);
    var post = await gh.PostAsync(prApi, commentReq);
    if (post.IsSuccessStatusCode)
    {
        Console.WriteLine("✅ Comment posted.");
    }
    else
    {
        Console.WriteLine($"❌ Failed to comment: {post.StatusCode}");
    }
}

static List<string> GetChangedFiles(string @base, string head)
{
    ProcessStartInfo psi = new()
    {
        FileName = "git",
        Arguments = $"diff --name-only origin/{@base}...origin/{head}",
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    var process = Process.Start(psi)!;
    var list = new List<string>();
    while (!process.StandardOutput.EndOfStream)
    {
        var line = process.StandardOutput.ReadLine();
        if (!string.IsNullOrWhiteSpace(line)) list.Add(line);
    }
    process.WaitForExit();
    return list;
}

static string GetFileDiff(string filePath, string @base, string head)
{
    ProcessStartInfo psi = new()
    {
        FileName = "git",
        Arguments = $"diff origin/{@base}...origin/{head} -- {filePath}",
        RedirectStandardOutput = true,
        UseShellExecute = false
    };
    var process = Process.Start(psi);
    if (process == null) return string.Empty;
    string diff = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return diff;
}
