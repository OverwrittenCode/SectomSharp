using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.SelectMenu;
using StrongInteractions.Attributes;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [StrongSelectMenuInteraction]
    private Task HelpSelectMenu(ulong id, HelpSelectMenuType type, string[] values)
        => HelpSelectMenuPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, type, values);

    [SlashCmd("Displays an interactive help menu")]
    public Task Help() => new HelpSelectMenuPaginationManager(_loggerFactory, Context) { IsEphemeral = true }.InitAsync(Context);
}
