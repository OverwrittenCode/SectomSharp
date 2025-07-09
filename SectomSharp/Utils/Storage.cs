using Discord;
using Discord.Interactions;

namespace SectomSharp.Utils;

internal static class Storage
{
    public const ulong ServerId = 944311981261881454;
    public const char ComponentWildcardSeparator = '_';

    public static readonly Dictionary<ICommandInfo, string> CommandInfoFullNameMap = [];

    public static readonly Color LightGold = new(0xe6c866);
}
