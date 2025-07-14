using System.Diagnostics.CodeAnalysis;
using Discord;
using NpgsqlTypes;

namespace SectomSharp.Data.CompositeTypes;

[PgName(PgName)]
public sealed record CompositeEmbedField([property: PgName("name")] string Name, [property: PgName("value")] string Value)
{
    public const string PgName = "embed_field";

    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public static List<EmbedFieldBuilder> ToBuilders(CompositeEmbedField[] fields)
    {
        List<EmbedFieldBuilder> builders = new(fields.Length);
        foreach (CompositeEmbedField field in fields)
        {
            builders.Add(new EmbedFieldBuilder { Name = field.Name, Value = field.Value });
        }

        return builders;
    }
}
