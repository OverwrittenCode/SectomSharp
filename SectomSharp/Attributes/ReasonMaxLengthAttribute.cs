using Discord.Interactions;
using SectomSharp.Data.Entities;

namespace SectomSharp.Attributes;

/// <summary>
///     Limit the maximum length of a reason parameter.
/// </summary>
public sealed class ReasonMaxLengthAttribute : MaxLengthAttribute
{
    public ReasonMaxLengthAttribute() : base(CaseConfiguration.ReasonMaxLength) { }
}
