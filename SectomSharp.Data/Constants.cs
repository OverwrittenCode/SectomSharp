namespace SectomSharp.Data;

internal static class Constants
{
    public const string ValueGeneratedOnAdd = "EF Core requires a setter to materialize the value from the database";

    public static class PostgreSql
    {
        public const string Timestamptz = "timestamptz";
        public const string Now = "NOW()";
        public const string JsonB = "jsonb";
    }
}
