// Program.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

string openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY not found");
string diffPath = "diff.patch";
if (!File.Exists(diffPath)) throw new Exception("diff.patch not found");

string diffContent = await File.ReadAllTextAsync(diffPath);

Console.WriteLine("📄 Diff patch loaded, length: " + diffContent.Length);

var requestBody = new
{
    model = "gpt-4",
    messages = new[]
    {
        new { role = "system", content = "你是一位資深工程助理，請簡要總結下面的 PR 差異：" },
        new { role = "user", content = diffContent }
    },
    temperature = 0.7
};

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiApiKey);

var response = await httpClient.PostAsync(
    "https://api.openai.com/v1/chat/completions",
    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
);

response.EnsureSuccessStatusCode();

var responseContent = await response.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(responseContent);
var summary = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

Console.WriteLine("\n🧠 GPT Summary:\n");
Console.WriteLine(summary);

await File.WriteAllTextAsync("output.txt", summary);
Console.WriteLine("\n💾 Summary written to output.txt");