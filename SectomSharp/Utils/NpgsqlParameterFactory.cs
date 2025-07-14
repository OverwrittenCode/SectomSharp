using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace SectomSharp.Utils;

[SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
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
    public static NpgsqlParameter<DateTimeOffset> FromDateTimeOffset(string name, DateTimeOffset value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.TimestampTz)
        => new(name, value) { NpgsqlDbType = npgsqlDbType };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<DateTimeOffset?> FromDateTimeOffset(string name, DateTimeOffset? value, NpgsqlDbType npgsqlDbType = NpgsqlDbType.TimestampTz)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int?> FromEnum32<T>(string name, T? value)
        where T : struct, Enum
    {
        T @enum = value.GetValueOrDefault();
        return new NpgsqlParameter<int?>(name, value.HasValue ? Unsafe.As<T, int>(ref @enum) : null) { NpgsqlDbType = NpgsqlDbType.Integer };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int[]> FromInt32Array(string name, int[] value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<int?[]> FromInt32Array(string name, int?[] value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<long[]> FromInt64Array(string name, long[] value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NpgsqlParameter<long?[]> FromInt64Array(string name, long?[] value) => new(name, value) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint };

    public static NpgsqlParameter<T[]> FromCompositeArray<T>(string name, T[] value, string pgName)
        => new(name, value)
        {
            DataTypeName = pgName
        };
}
