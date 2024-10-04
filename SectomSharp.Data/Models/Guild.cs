namespace SectomSharp.Data.Models;

public sealed class Guild : BaseEntity
{
    public required ulong Id { get; init; }

    public ICollection<User> Users { get; } = [];
}
