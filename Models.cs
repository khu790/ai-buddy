namespace AiBuddy;

public sealed record BuddySettings(string Provider, string Model, string OllamaUrl, bool WatchOnStart)
{
    public static BuddySettings Default { get; } = new("Ollama", "qwen3-vl:2b", "http://localhost:11434", false);
}

public sealed record ChatTurn(string Role, string Text);
