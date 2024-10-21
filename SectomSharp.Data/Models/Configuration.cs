namespace SectomSharp.Data.Models;

public sealed class Configuration
{
    public int Id { get; set; }

    public WarningConfiguration? Warning { get; set; }
}
