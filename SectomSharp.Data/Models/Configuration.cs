namespace SectomSharp.Data.Models;

public sealed class Configuration
{
    public WarningConfiguration Warning { get; init; } = new();
}
