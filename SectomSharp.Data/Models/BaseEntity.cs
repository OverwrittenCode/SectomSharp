namespace SectomSharp.Data.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; internal set; }
    public DateTime? UpdatedAt { get; internal set; }
}
