namespace SectomSharp.Data.Models;

public sealed class Channel : Snowflake
{
    public ICollection<Case> Cases { get; } = [];
}
