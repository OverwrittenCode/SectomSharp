using JetBrains.Annotations;

namespace SectomSharp.Data.Models;

public sealed class User : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> PerpetratorCases { get; private set; } = [];
    
    [UsedImplicitly]
    public ICollection<Case> TargetCases { get; } = [];
}
