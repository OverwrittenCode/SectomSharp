namespace SectomSharp.Data.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; internal set; }
}
