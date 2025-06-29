using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Add a moderation note to a user in the server")]
    public async Task ModNote([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string note)
    {
        await DeferAsync();
        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.ModNote, OperationType.Create, user.Id, reason: note);
    }
}
