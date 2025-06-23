using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("leveling", "Leveling configuration")]
        public sealed class LevelingModule : DisableableModule<LevelingModule, LevelingConfiguration>
        {
            /// <inheritdoc />
            public LevelingModule(ILogger<BaseModule<LevelingModule>> logger) : base(logger) { }

            private async Task ModifySettingsAsync(string? reason, bool? accumulateMultipliers = null, double? globalMultiplier = null, uint? globalCooldown = null)
            {
                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild guild = await EnsureGuildAsync(db);

                    LevelingConfiguration config = guild.Configuration.Leveling;
                    if (accumulateMultipliers.HasValue && accumulateMultipliers.Value != config.AccumulateMultipliers)
                    {
                        config.AccumulateMultipliers = accumulateMultipliers.Value;
                    }
                    else if (globalMultiplier.HasValue && Math.Abs(globalMultiplier.Value - config.GlobalMultiplier) > Double.Epsilon)
                    {
                        config.GlobalMultiplier = globalMultiplier.Value;
                    }
                    else if (globalCooldown.HasValue && globalCooldown.Value != config.GlobalCooldown)
                    {
                        config.GlobalCooldown = globalCooldown.Value;
                    }
                    else
                    {
                        await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            [SlashCmd("Set if multipliers should accumulate")]
            public async Task SetAccumulateMultipliers(bool accumulateMultipliers, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, accumulateMultipliers);

            [SlashCmd("Set the global multiplier")]
            public async Task SetGlobalMultiplier([MinValue(1)] double globalMultiplier, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, globalMultiplier: globalMultiplier);

            [SlashCmd("Set the global cooldown in seconds")]
            public async Task SetGlobalCooldown([MinValue(1)] uint globalCooldown, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, globalCooldown: globalCooldown);

            [SlashCmd("Adds an auto role on reaching a certain level")]
            public async Task AddAutoRole(
                SocketRole role,
                [Summary(description: "The level that grants them the role")] [MinValue(1)] uint level,
                [Summary(description: "The overriden multiplier")] [MinValue(1)] double? multiplier = null,
                [Summary(description: "The overriden cooldown in seconds")] [MinValue(1)] [MaxValue(60)] uint? cooldown = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild guild = await EnsureGuildAsync(db);

                    if (guild.Configuration.Leveling.AutoRoles.Exists(x => x.Level == level || x.Id == role.Id))
                    {
                        await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    guild.Configuration.Leveling.AutoRoles.Add(
                        new LevelingRole
                        {
                            Id = role.Id,
                            Level = level,
                            Multiplier = multiplier,
                            Cooldown = cooldown
                        }
                    );
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            [SlashCmd("Removes an auto role for a certain level")]
            public async Task RemoveAutoRole([Summary(description: "The level to remove")] [MinValue(1)] uint level, [ReasonMaxLength] string? reason = null)
            {
                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
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
                        await RespondOrFollowupAsync(NotConfiguredMessage);
                        return;
                    }

                    LevelingRole? match = guild.Configuration.Leveling.AutoRoles.Find(x => x.Level == level);

                    if (match is null)
                    {
                        await RespondOrFollowupAsync(NotConfiguredMessage);
                        return;
                    }

                    guild.Configuration.Leveling.AutoRoles.Remove(match);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            [SlashCmd("View the configured auto roles")]
            public async Task ViewAutoRoles()
            {
                if (await TryGetConfigurationViewAsync() is not var (levelingConfiguration, embedBuilder))
                {
                    return;
                }

                if (levelingConfiguration.AutoRoles.Count == 0)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                embedBuilder.WithTitle($"{Context.Guild.Name} Leveling Auto Roles")
                            .WithDescription(String.Join('\n', levelingConfiguration.AutoRoles.OrderBy(autoRole => autoRole.Level).Select(autoRole => autoRole.Display())));

                await RespondOrFollowupAsync(embeds: [embedBuilder.Build()]);
            }
        }
    }
}
