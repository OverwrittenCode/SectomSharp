using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Managers.Pagination.SelectMenu;

namespace SectomSharp.Modules.Pagination;

public sealed class PaginationModule : InteractionModuleBase<SocketInteractionContext>
{
    /// <inheritdoc cref="ButtonPaginationManager.OnHit(SocketMessageComponent, String, PageNavigationButton)"/>
    [RegexComponentInteraction<ButtonPaginationManager>("id", "position")]
    public async Task Button(string id, PageNavigationButton position) =>
        await ButtonPaginationManager.OnHit(
            (SocketMessageComponent)Context.Interaction,
            id,
            position
        );

    /// <inheritdoc cref="SelectMenuPaginationManager.OnHit(SocketMessageComponent, String, String[])"/>
    [RegexComponentInteraction<SelectMenuPaginationManager>("id")]
    public async Task SelectMenu(string id, string[] values) =>
        await SelectMenuPaginationManager.OnHit(
            (SocketMessageComponent)Context.Interaction,
            id,
            values
        );
}
