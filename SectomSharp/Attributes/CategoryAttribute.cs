using System.Diagnostics;
using Discord;

namespace SectomSharp.Attributes;

/// <summary>
///     Categorise commands under a module.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[DebuggerDisplay("{Name} ({Emoji.Name})")]
internal sealed class CategoryAttribute : Attribute
{
    /// <summary>
    ///     Gets the name of the category.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the emoji associated with the category.
    /// </summary>
    public Emoji Emoji { get; }

    /// <summary>
    ///     Extend a module by categorising the commands attached to it.
    /// </summary>
    /// <param name="name">The name of the category.</param>
    /// <param name="unicode">The Unicode associated with the category.</param>
    public CategoryAttribute(string name, string unicode)
    {
        Name = name;
        Emoji = new Emoji(unicode);
    }
}
