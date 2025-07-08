namespace SectomSharp.Graphics;

public sealed class LeaderboardPlayer
{
    public static readonly LeaderboardPlayer Unknown = new()
    {
        DisplayName = "???",
        Username = "???",
        Level = 0,
        Xp = 0,
        AvatarUrl = ""
    };

    public required string DisplayName { get; init; }
    public required string Username { get; init; }
    public required uint Level { get; init; }
    public required uint Xp { get; init; }
    public required string AvatarUrl { get; init; }
}
