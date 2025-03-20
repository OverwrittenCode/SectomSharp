using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Managers.Pagination.SelectMenu;

namespace SectomSharp.Modules.Pagination;

public sealed class PaginationModule : BaseModule
{
    /// <inheritdoc cref="ButtonPaginationManager.OnHit(SocketMessageComponent, String, PageNavigationButton)" />
    [RegexComponentInteraction<ButtonPaginationManager>(nameof(id), nameof(position))]
    public async Task Button(string id, PageNavigationButton position) => await ButtonPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, position);

    /// <inheritdoc cref="SelectMenuPaginationManager.OnHit(SocketMessageComponent, String, String[])" />
    [RegexComponentInteraction<SelectMenuPaginationManager>(nameof(id))]
    public async Task SelectMenu(string id, string[] values) => await SelectMenuPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, values);
}
