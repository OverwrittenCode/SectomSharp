namespace SectomSharp.Data.Entities;

public sealed class Configuration
{
    public WarningConfiguration Warning { get; init; } = new();
}
