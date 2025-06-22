using System.Text;
using Discord;

namespace SectomSharp.Data.Entities;

public sealed class LevelingRole
{
    public required ulong Id { get; init; }
    public required uint Level { get; init; }
    public double? Multiplier { get; init; }
    public uint? Cooldown { get; init; }

    public string Display()
    {
        var builder = new StringBuilder($"- Level {Level}: {MentionUtils.MentionRole(Id)}", 50);
        if (Multiplier.HasValue)
        {
            builder.Append($" (x{Multiplier.Value:F2})");
        }

        if (Cooldown.HasValue)
        {
            builder.Append($" ({Cooldown.Value:D2}s)");
        }

        return builder.ToString();
    }
}
