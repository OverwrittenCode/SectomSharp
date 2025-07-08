using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Modules.Pagination;

public sealed class PaginationModule : BaseModule<PaginationModule>
{
    /// <inheritdoc />
    public PaginationModule(ILogger<PaginationModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

    [RegexComponentInteraction<ButtonPaginationManager>]
    public async Task Button(ulong id, PageNavigationButton position) => await ButtonPaginationManager.OnHit((SocketMessageComponent)Context.Interaction, id, position);
}
