using System.Data.Common;

namespace SectomSharp.Extensions;

public static class DbCommandExtensions
{
    public static async Task<T> ExecuteScalarAsync<T>(this DbCommand cmd)
        => await cmd.ExecuteScalarAsync() is T result ? result : throw new InvalidCastException($"Returned value is not of type {typeof(T)}");
}
