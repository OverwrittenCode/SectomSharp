using JetBrains.Annotations;

namespace SectomSharp.Data.Entities;

public sealed class UserLevel
{
    public DateTimeOffset? UpdatedAt { get; [UsedImplicitly] set; }

    public uint CurrentXp { get; [UsedImplicitly] set; }
}
