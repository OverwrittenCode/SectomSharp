using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace SectomSharp.Utils;

public static class NpgsqlParameterFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int> FromInt32(string name, int value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int?> FromInt32(string name, int? value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int> FromNonNegativeInt32(string name, uint value) => new(name, (int)value) { NpgsqlDbType = NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int?> FromNonNegativeInt32(string name, uint? value) => new(name, (int?)value) { NpgsqlDbType = NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<double> FromDouble(string name, double value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Double };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<double?> FromDouble(string name, double? value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Double };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<long> FromSnowflakeId(string name, ulong value) => new(name, (long)value) { NpgsqlDbType = NpgsqlDbType.Bigint };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<long?> FromSnowflakeId(string name, ulong? value) => new(name, (long?)value) { NpgsqlDbType = NpgsqlDbType.Bigint };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<bool> FromBoolean(string name, bool value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Boolean };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<bool?> FromBoolean(string name, bool? value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Boolean };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<string?> FromVarchar(string name, string? value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Varchar };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<string?> FromJsonB(string name, string? value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Jsonb };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<DateTime> FromDateTime(string name, DateTime value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.TimestampTz)
        => new(name, value) { NpgsqlDbType = npgsqlDbType };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<DateTime?> FromDateTime(string name, DateTime? value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.TimestampTz)
        => new(name, value) { NpgsqlDbType = npgsqlDbType };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<TimeSpan> FromTimeSpan(string name, TimeSpan value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.Interval)
        => new(name, value) { NpgsqlDbType = npgsqlDbType };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<TimeSpan?> FromTimeSpan(string name, TimeSpan? value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.Interval)
        => new(name, value) { NpgsqlDbType = npgsqlDbType };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int> FromEnum32<T>(string name, T value)
        where T : struct, Enum
        => new(name, Unsafe.As<T, int>(ref value)) { NpgsqlDbType = NpgsqlDbType.Integer };
}
