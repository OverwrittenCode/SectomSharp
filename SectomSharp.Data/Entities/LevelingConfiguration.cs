using JetBrains.Annotations;

namespace SectomSharp.Data.Entities;

public sealed class LevelingConfiguration : BaseConfiguration
{
    public bool AccumulateMultipliers { get; set; }
    public double GlobalMultiplier { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] set; }
    public uint GlobalCooldown { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] set; }
}
