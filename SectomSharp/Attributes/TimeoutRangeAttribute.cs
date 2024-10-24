using Discord;
using Discord.Interactions;

namespace SectomSharp.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class TimeoutRangeAttribute : ParameterPreconditionAttribute
{
    private static readonly TimeSpan Min = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Max = TimeSpan.FromDays(28);

    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        IParameterInfo parameterInfo,
        object value,
        IServiceProvider services
    )
    {
        if (value is not TimeSpan timeSpan)
        {
            return Task.FromResult(
                PreconditionResult.FromError("Expected a timespan for the duration.")
            );
        }

        if (timeSpan < Min)
        {
            return Task.FromResult(
                PreconditionResult.FromError($"Duration cannot be less than {Min.Seconds}")
            );
        }

        if (timeSpan > Max)
        {
            return Task.FromResult(
                PreconditionResult.FromError($"Duration cannot exceed {Max.Days}")
            );
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
