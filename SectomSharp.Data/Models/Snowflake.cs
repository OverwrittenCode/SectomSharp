namespace SectomSharp.Data.Models;

public class Snowflake : BaseOneToManyGuildRelation
{
    public required ulong Id { get; init; }
}
