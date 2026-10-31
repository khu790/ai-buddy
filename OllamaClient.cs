using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AiBuddy;

public sealed class OllamaClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public async Task<string> AskAsync(
        string baseUrl,
        string model,
        string prompt,
        byte[] jpeg,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Enter a valid Ollama URL, such as http://localhost:11434.");

        var messages = new List<object>
        {
            new { role = "system", content = "You are AI Buddy, a helpful desktop companion. Answer the user's question using the attached screenshot when relevant. Be concise, practical, and clear. Never claim to have performed actions on the computer." }
        };
        if (history is not null)
        {
            foreach (var turn in history.TakeLast(8))
                messages.Add(new { role = turn.Role, content = turn.Text });
        }
        messages.Add(new
        {
            role = "user",
            content = prompt,
            images = new[] { Convert.ToBase64String(jpeg) }
        });

        var payload = JsonSerializer.Serialize(new { model, messages, stream = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "api/chat"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Ollama did not respond in time. The first model response can take a few minutes while it loads.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Can't reach Ollama. Install and start Ollama, then download the model with: ollama run " + model, ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string? message = null;
                try { message = JsonDocument.Parse(body).RootElement.GetProperty("error").GetString(); } catch { }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new InvalidOperationException($"Ollama model '{model}' is not installed. Open Ollama and run: ollama run {model}");
                throw new InvalidOperationException(message ?? $"Ollama request failed ({(int)response.StatusCode} {response.ReasonPhrase}).");
            }

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var result = contentElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
            return "I couldn't produce a text response. Please try asking another way.";
        }
    }
}

public static class AiAssistant
{
    public static Task<string> AskAsync(
        BuddySettings settings,
        string prompt,
        byte[] jpeg,
        IReadOnlyList<ChatTurn>? history = null,
        CancellationToken cancellationToken = default)
    {
        if (settings.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            return new OpenAiClient(settings.Model).AskAsync(prompt, jpeg, history, cancellationToken);

        return new OllamaClient().AskAsync(settings.OllamaUrl, settings.Model, prompt, jpeg, history, cancellationToken);
    }
}
