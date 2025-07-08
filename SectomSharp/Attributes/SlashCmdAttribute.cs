using System.Runtime.CompilerServices;
using Discord.Interactions;
using JetBrains.Annotations;
using SectomSharp.Utils;

namespace SectomSharp.Attributes;

/// <inheritdoc />
[MeansImplicitUse]
public sealed class SlashCmdAttribute : SlashCommandAttribute
{
    /// <summary>
    ///     Register a method as a Slash Command with the name set to the <see cref="CallerMemberNameAttribute" /> with <see cref="StringUtils.PascalCaseToKebabCase" /> casing.
    /// </summary>
    /// <param name="description">The description of the command.</param>
    /// <param name="ignoreGroupNames">
    ///     If <see cref="GroupAttribute" />s will be ignored while creating this command and this method will be treated as a top level command.
    /// </param>
    /// <param name="runMode">The run mode of the command.</param>
    public SlashCmdAttribute(string description, bool ignoreGroupNames = false, RunMode runMode = RunMode.Default, [CallerMemberName] string name = "") : this(
        StringUtils.PascalCaseToKebabCase(name),
        description,
        ignoreGroupNames,
        runMode
    ) { }

    /// <summary>
    ///     Register a method as a Slash Command.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">The description of the command.</param>
    /// <param name="ignoreGroupNames">
    ///     If <see cref="GroupAttribute" />s will be ignored while creating this command and this method will be treated as a top level command.
    /// </param>
    /// <param name="runMode">The run mode of the command.</param>
    public SlashCmdAttribute(string name, string description, bool ignoreGroupNames = false, RunMode runMode = RunMode.Default) : base(
        name,
        description,
        ignoreGroupNames,
        runMode
    ) { }
}
