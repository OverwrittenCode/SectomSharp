using Discord;
using Discord.Interactions;

namespace SectomSharp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class TimeoutRangeAttribute : ParameterPreconditionAttribute
{
    private static readonly TimeSpan Min = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Max = TimeSpan.FromDays(28);

    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, IParameterInfo parameterInfo, object value, IServiceProvider services)
        => value is not TimeSpan timeSpan
            ? Task.FromResult(PreconditionResult.FromError("Expected a timespan for the duration."))
            : timeSpan < Min
                ? Task.FromResult(PreconditionResult.FromError($"Duration cannot be less than {Min.Seconds}"))
                : Task.FromResult(timeSpan > Max ? PreconditionResult.FromError($"Duration cannot exceed {Max.Days}") : PreconditionResult.FromSuccess());
}
