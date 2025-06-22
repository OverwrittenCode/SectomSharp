namespace SectomSharp.Data.Entities;

public sealed class Configuration
{
    public WarningConfiguration Warning { get; init; } = new();
    public LevelingConfiguration Leveling { get; init; } = new();
}
