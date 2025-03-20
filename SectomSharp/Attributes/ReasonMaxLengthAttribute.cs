using Discord.Interactions;

namespace SectomSharp.Attributes;

/// <inheritdoc />
public sealed class ReasonMaxLengthAttribute : MaxLengthAttribute
{
    private const int MaxLength = 250;

    public ReasonMaxLengthAttribute() : base(MaxLength) { }
}
