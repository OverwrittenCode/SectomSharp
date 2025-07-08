using JetBrains.Annotations;

namespace SectomSharp.Data.Entities;

public sealed class UserLevel
{
    public DateTime? UpdatedAt { get; [UsedImplicitly] set; }

    public uint CurrentXp { get; [UsedImplicitly] set; }
}
