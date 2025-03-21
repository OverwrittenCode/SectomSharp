using JetBrains.Annotations;

namespace SectomSharp.Data.Models;

public sealed class Channel : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> Cases { get; } = [];
}
