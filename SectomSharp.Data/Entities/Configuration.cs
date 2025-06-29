namespace SectomSharp.Data.Entities;

public sealed class Configuration
{
    public WarningConfiguration Warning { get; } = new();
    public LevelingConfiguration Leveling { get; } = new();
}
