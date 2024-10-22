namespace SectomSharp.Data.Models;

public sealed class WarningConfiguration : BaseConfiguration
{
    public List<WarningThreshold> Thresholds { get; set; } = [];
}
