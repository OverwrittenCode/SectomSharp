using System.Runtime.CompilerServices;
using Discord;
using SectomSharp.Extensions;

namespace SectomSharp.Utils;

internal static class EmbedFieldBuilderFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EmbedFieldBuilder Create<T>(string name, T value)
        where T : notnull
        => new()
        {
            Name = name,
            Value = value
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EmbedFieldBuilder CreateTruncated(string name, string value)
        => new()
        {
            Name = name,
            Value = value.Truncate(EmbedFieldBuilder.MaxFieldValueLength)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EmbedFieldBuilder CreateInlined<T>(string name, T value)
        => new()
        {
            Name = name,
            Value = value,
            IsInline = true
        };
}
