namespace AiBuddy;

public sealed record BuddySettings(string Model, bool WatchOnStart)
{
    public static BuddySettings Default { get; } = new("gpt-6-astra", false);
}

public sealed record ChatTurn(string Role, string Text);
