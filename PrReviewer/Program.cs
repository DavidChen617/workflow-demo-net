// Program.cs
using System.Diagnostics;
using System.Text;
using System.Text.Json;

string openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY not found");
string head = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF") ?? throw new Exception("GITHUB_HEAD_REF not found");
string @base = Environment.GetEnvironmentVariable("GITHUB_BASE_REF") ?? throw new Exception("GITHUB_BASE_REF not found");

Console.WriteLine($"🔧 GITHUB_BASE_REF: {@base}");
Console.WriteLine($"🔧 GITHUB_HEAD_REF: {head}");

string[] allowedExtensions = new[] { ".cs", ".ts", ".js" };

Console.WriteLine("🔍 Fetching changed files...");

ProcessStartInfo psi = new()
{
    FileName = "git",
    Arguments = $"diff --name-only origin/{@base}...origin/{head}",
    RedirectStandardOutput = true,
    UseShellExecute = false
};

var process = Process.Start(psi);
if (process == null) throw new Exception("Failed to run git diff");
List<string> changedFiles = new();
while (!process.StandardOutput.EndOfStream)
{
    var line = process.StandardOutput.ReadLine();
    if (!string.IsNullOrWhiteSpace(line) && allowedExtensions.Any(ext => line.EndsWith(ext)))
    {
        changedFiles.Add(line);
    }
}
process.WaitForExit();

if (changedFiles.Count == 0)
{
    Console.WriteLine("✅ No target files changed. Nothing to analyze.");
    return;
}

Console.WriteLine($"📁 {changedFiles.Count} file(s) matched extensions: {string.Join(", ", changedFiles)}");

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new("Bearer", openAiApiKey);
var outputBuilder = new StringBuilder();

foreach (var file in changedFiles)
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

    Console.WriteLine("🤖 Calling GPT...");

    var requestBody = new
    {
        model = "gpt-4",
        messages = new[]
        {
            new { role = "system", content = "你是一位資深工程助理，請根據提供的原始檔與變更進行評論建議。" },
            new { role = "user", content = $"原始程式碼：\n{fileContent}\n\n改動內容：\n{fileDiff}" }
        },
        temperature = 0.7
    };

    var response = await httpClient.PostAsync(
        "https://api.openai.com/v1/chat/completions",
        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
    );

    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(responseContent);
    var summary = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

    Console.WriteLine("🧠 GPT 回應：\n" + summary);
    outputBuilder.AppendLine($"==== {file} ====");
    outputBuilder.AppendLine(summary);
    outputBuilder.AppendLine();
}

await File.WriteAllTextAsync("output.txt", outputBuilder.ToString());
Console.WriteLine("💾 所有分析結果已寫入 output.txt");

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
