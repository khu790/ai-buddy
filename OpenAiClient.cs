using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AiBuddy;

public sealed class OpenAiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };
    private readonly string _model;
    private readonly string _key;

    public OpenAiClient(string model)
    {
        _model = string.IsNullOrWhiteSpace(model) ? BuddySettings.Default.Model : model.Trim();
        _key = CredentialStore.Read() ?? throw new InvalidOperationException("Add your OpenAI API key in Settings to use AI Buddy.");
    }

    public async Task<string> AskAsync(string prompt, byte[] jpeg, IReadOnlyList<ChatTurn>? history = null, CancellationToken cancellationToken = default)
    {
        var input = new List<object>();
        if (history is not null)
        {
            foreach (var turn in history.TakeLast(8))
                input.Add(new { role = turn.Role, content = turn.Text });
        }
        input.Add(new
        {
            role = "user",
            content = new object[]
            {
                new { type = "input_text", text = prompt },
                new { type = "input_image", image_url = "data:image/jpeg;base64," + Convert.ToBase64String(jpeg), detail = "auto" }
            }
        });

        var payload = JsonSerializer.Serialize(new { model = _model, input, store = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string? message = null;
            try { message = JsonDocument.Parse(body).RootElement.GetProperty("error").GetProperty("message").GetString(); } catch { }
            throw new InvalidOperationException(message ?? $"OpenAI request failed ({(int)response.StatusCode} {response.ReasonPhrase}). Check the API key, model name, and network connection.");
        }

        using var document = JsonDocument.Parse(body);
        var output = new StringBuilder();
        if (document.RootElement.TryGetProperty("output", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
                foreach (var part in content.EnumerateArray())
                    if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" && part.TryGetProperty("text", out var text))
                        output.AppendLine(text.GetString());
            }
        }
        var result = output.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "I couldn't produce a text response. Try asking in a different way." : result;
    }
}
