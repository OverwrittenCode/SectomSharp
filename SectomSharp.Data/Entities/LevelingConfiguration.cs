namespace SectomSharp.Data.Entities;

public sealed class LevelingConfiguration : BaseConfiguration
{
    public List<LevelingRole> AutoRoles { get; } = [];
    public bool AccumulateMultipliers { get; set; }

    public double GlobalMultiplier { get; set; } = 1;
    public uint GlobalCooldown { get; set; } = 3;
}
