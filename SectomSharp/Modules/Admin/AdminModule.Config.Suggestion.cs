using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("suggestion", "Suggestion configuration")]
        public sealed partial class SuggestionModule : DisableableModule<SuggestionModule>, IDisableableModule<SuggestionModule>
        {
            /// <inheritdoc />
            public static string DisableColumnName => "Configuration_Suggestion_IsDisabled";

            /// <inheritdoc />
            public SuggestionModule(ILogger<SuggestionModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }
        }
    }
}
