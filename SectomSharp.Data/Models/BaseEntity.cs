namespace SectomSharp.Data.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; internal set; } = DateTime.UtcNow;
}
