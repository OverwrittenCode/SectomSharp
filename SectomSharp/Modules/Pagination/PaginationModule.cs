using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Managers.Pagination.Button;
using StrongInteractions.Attributes;

namespace SectomSharp.Modules.Pagination;

public sealed partial class PaginationModule : BaseModule<PaginationModule>
{
    /// <inheritdoc />
    public PaginationModule(ILogger<PaginationModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

    [StrongButtonInteraction]
    private async Task Button(ulong id, PageNavigationButton position) => await ButtonPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, position);
}
