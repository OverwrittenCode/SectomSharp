using System.Runtime.CompilerServices;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    [Group("config", "Master configuration of the server")]
    public sealed partial class ConfigModule : BaseModule<ConfigModule>
    {
        private const string AlreadyConfiguredMessage = "You cannot add this new configuration as there is already a matching configuration.";
        private const string NotConfiguredMessage = "You cannot remove this configuration as it has not been configured.";
        private const string NothingToView = "Nothing to view yet.";

        private static async Task LogAsync(SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => await CaseUtils.LogAsync(context, BotLogType.Configuration, OperationType.Update, channelId: channelId, reason: reason);

        /// <inheritdoc />
        public ConfigModule(ILogger<ConfigModule> logger) : base(logger) { }

        /// <summary>
        ///     Provides a base class for a disableable config command module to inherit from.
        /// </summary>
        /// <typeparam name="TThis">Type of interaction context to be injected into the module.</typeparam>
        /// <typeparam name="TConfig">Type of <see cref="BaseConfiguration" /> in <see cref="Configuration" />.</typeparam>
        public abstract class DisableableModule<TThis, TConfig> : BaseModule<TThis>
            where TConfig : BaseConfiguration
            where TThis : DisableableModule<TThis, TConfig>
        {
            private static string ModuleIsCurrentlyDisabledMessage
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    string path;
                    if (typeof(TConfig) == typeof(WarningConfiguration))
                    {
                        path = "/config warn enable";
                    }
                    else if (typeof(TConfig) == typeof(LevelingConfiguration))
                    {
                        path = "/config leveling enable";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return $"Module is currently disabled: {path}";
                }
            }

            private static TConfig GetConfig(Guild guild)
            {
                if (typeof(TConfig) == typeof(WarningConfiguration))
                {
                    WarningConfiguration configuration = guild.Configuration.Warning;
                    return Unsafe.As<WarningConfiguration, TConfig>(ref configuration);
                }

                if (typeof(TConfig) == typeof(LevelingConfiguration))
                {
                    LevelingConfiguration configuration = guild.Configuration.Leveling;
                    return Unsafe.As<LevelingConfiguration, TConfig>(ref configuration);
                }

                throw new NotSupportedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static async Task SetIsDisabledAsync(SocketInteractionContext context, bool isDisabled, string? reason)
            {
                await context.Interaction.DeferAsync();
                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db.Guilds.FindAsync(context.Guild.Id);

                    if (guild is null)
                    {
                        Configuration configuration;
                        if (typeof(TConfig) == typeof(WarningConfiguration))
                        {
                            configuration = new Configuration
                            {
                                Warning = new WarningConfiguration
                                {
                                    IsDisabled = isDisabled
                                }
                            };
                        }
                        else if (typeof(TConfig) == typeof(LevelingConfiguration))
                        {
                            configuration = new Configuration
                            {
                                Leveling = new LevelingConfiguration
                                {
                                    IsDisabled = isDisabled
                                }
                            };
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        await db.Guilds.AddAsync(
                            new Guild
                            {
                                Id = context.Guild.Id,
                                Configuration = configuration
                            }
                        );

                        await db.SaveChangesAsync();
                        await LogAsync(context, reason);
                        return;
                    }

                    TConfig config = GetConfig(guild);
                    if (config.IsDisabled == isDisabled)
                    {
                        await context.Interaction.RespondOrFollowupAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    config.IsDisabled = isDisabled;
                    await db.SaveChangesAsync();
                }

                await LogAsync(context, reason);
            }

            /// <inheritdoc />
            protected DisableableModule(ILogger<BaseModule<TThis>> logger) : base(logger) { }

            protected async Task<(TConfig Config, EmbedBuilder EmbedBuilder)?> TryGetConfigurationViewAsync()
            {
                await DeferAsync();

                await using var db = new ApplicationDbContext();

                Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new Guild
                        {
                            Id = Context.Guild.Id
                        }
                    );
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NothingToView);
                    return null;
                }

                db.Entry(guild).State = EntityState.Detached;

                TConfig config = GetConfig(guild);
                EmbedBuilder embedBuilder = new EmbedBuilder().WithColor(Storage.LightGold);
                if (config.IsDisabled)
                {
                    embedBuilder.WithFooter(ModuleIsCurrentlyDisabledMessage);
                }

                return (config, embedBuilder);
            }

            [SlashCmd("Disable this configuration")]
            public async Task Disable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(Context, true, reason);

            [SlashCmd("Enable this configuration")]
            public async Task Enable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(Context, false, reason);
        }
    }
}
