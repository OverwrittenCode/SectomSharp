using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Utils;

namespace SectomSharp.Modules;

/// <inheritdoc />
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public abstract class BaseModule<TThis> : InteractionModuleBase<SocketInteractionContext>
    where TThis : BaseModule<TThis>
{
    protected const string TimespanDescription = "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)";
    protected const string NothingToView = "Nothing to view yet.";

    private static readonly Func<ILogger, string, string, string, string, IDisposable?> GuildCommandScopeCallback =
        LoggerMessage.DefineScope<string, string, string, string>("Command={CommandName}, User={Username}, Guild={GuildName}, Channel={ChannelName}");

    private static readonly Func<ILogger, string, string, string, IDisposable?> CommandScopeCallback =
        LoggerMessage.DefineScope<string, string, string>("Command={CommandName}, User={Username}, Channel={ChannelName}");

    private IDisposable? _logScope;

    protected readonly ILogger<BaseModule<TThis>> Logger;

    /// <summary>
    ///     Gets the db factory.
    /// </summary>
    protected IDbContextFactory<ApplicationDbContext> DbContextFactory { get; }

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The factory for creating <see cref="ApplicationDbContext" /> instances.</param>
    protected BaseModule(ILogger<BaseModule<TThis>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        Logger = logger;
        DbContextFactory = dbContextFactory;
    }

    public override void BeforeExecute(ICommandInfo command)
    {
        string fullName = Storage.CommandInfoFullNameMap[command];
        _logScope = Context.Guild == null
            ? CommandScopeCallback(Logger, fullName, Context.User.Username, Context.Channel.Name)
            : GuildCommandScopeCallback(Logger, fullName, Context.User.Username, Context.Guild.Name, Context.Channel.Name);

        base.BeforeExecute(command);
    }

    public override void AfterExecute(ICommandInfo command)
    {
        if (_logScope is not null)
        {
            _logScope.Dispose();
            _logScope = null;
        }

        base.AfterExecute(command);
    }
}
