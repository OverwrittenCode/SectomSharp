using Discord;
using Discord.Interactions;
using SectomSharp.Managers.Pagination.SelectMenu;

namespace SectomSharp.Attributes;

/// <summary>
///     Mark a parameter as a <see cref="Managers.InstanceManager{T}.Id" /> for <see cref="SelectMenuPaginationManager" />.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class SelectMenuPaginationInstanceIdAttribute : ParameterPreconditionAttribute
{
    public override string ErrorMessage => SelectMenuPaginationManager.PaginationExpiredMessage;

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        IParameterInfo parameterInfo,
        object value,
        IServiceProvider services
    )
    {
        if (value is not string instanceId)
        {
            return PreconditionResult.FromError("Expected a string value for the instance id.");
        }

        if (
            !SelectMenuPaginationManager.AllInstances.TryGetValue(
                instanceId,
                out SelectMenuPaginationManager? instance
            )
        )
        {
            return PreconditionResult.FromError(ErrorMessage);
        }

        try
        {
            await instance.RestartTimer();
        }
        catch (ObjectDisposedException)
        {
            // race condition from instance disposal
        }

        return PreconditionResult.FromSuccess();
    }
}
