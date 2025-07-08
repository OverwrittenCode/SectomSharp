using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.SelectMenu;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCmd("Displays an interactive help menu")]
    public Task Help() => new HelpSelectMenuPaginationManager(_loggerFactory, Context) { IsEphemeral = true }.InitAsync(Context);

    [RegexComponentInteraction<HelpSelectMenuPaginationManager>]
    public async Task HelpSelectMenu(ulong id, HelpSelectMenuType type, string[] values)
        => await HelpSelectMenuPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, type, values);
}
