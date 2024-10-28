namespace SectomSharp.Data.Models;

public abstract class Snowflake : BaseOneToManyGuildRelation
{
    public required ulong Id { get; init; }
}
