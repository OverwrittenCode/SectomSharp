namespace SectomSharp.Data.Models;

public sealed class User : Snowflake
{
    public ICollection<Case> PerpetratorCases { get; } = [];
    public ICollection<Case> TargetCases { get; } = [];
}
