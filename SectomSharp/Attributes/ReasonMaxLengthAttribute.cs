using Discord.Interactions;
using SectomSharp.Data.Configurations;

namespace SectomSharp.Attributes;

/// <inheritdoc />
public sealed class ReasonMaxLengthAttribute : MaxLengthAttribute
{
    public ReasonMaxLengthAttribute() : base(CaseConfiguration.ReasonMaxLength) { }
}
