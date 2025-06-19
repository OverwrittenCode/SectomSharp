namespace SectomSharp.Data.Entities;

public sealed class WarningConfiguration : BaseConfiguration
{
    public List<WarningThreshold> Thresholds { get; init; } = [];
}
