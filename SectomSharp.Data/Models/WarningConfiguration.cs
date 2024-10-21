namespace SectomSharp.Data.Models;

public sealed class WarningConfiguration : BaseConfiguration
{
    /// <summary>
    ///     Gets or sets the geometric multiplier for increasing the punishment duration.
    ///     <para/>
    ///         When a user receives the same punishment consecutively, the duration of that punishment
    ///         is calculated using the formula:
    ///         <code>GeometricDurationMultiplier ** RepeatedOffences * BasePunishmentDuration</code>
    /// </summary>
    public int GeometricDurationMultiplier { get; set; } = 1;
    public List<WarningThreshold> Thresholds { get; set; } = [];
}
