using System.Runtime.CompilerServices;
using Discord.Interactions;
using SectomSharp.Utils;

namespace SectomSharp.Attributes;

/// <inheritdoc />
public sealed class SlashCmdAttribute : SlashCommandAttribute
{
    /// <inheritdoc />
    public SlashCmdAttribute(string description, bool ignoreGroupNames = false, RunMode runMode = RunMode.Default, [CallerMemberName] string name = "") : base(
        StringUtils.PascalCaseToKebabCase(name),
        description,
        ignoreGroupNames,
        runMode
    ) { }
}
