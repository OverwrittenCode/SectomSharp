namespace SectomSharp.Data.Entities;

public abstract class Snowflake : BaseOneToManyGuildRelation
{
    public required ulong Id { get; init; }
}
