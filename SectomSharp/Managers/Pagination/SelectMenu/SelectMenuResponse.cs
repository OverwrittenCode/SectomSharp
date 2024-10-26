namespace SectomSharp.Managers.Pagination.SelectMenu;

/// <summary>
///     Specifies how the select menu pagination should respond to user interactions.
/// </summary>
internal enum SelectMenuResponse
{
    /// <summary>
    ///     The select menu should use <see cref="Discord.WebSocket.SocketInteraction.RespondAsync" />.
    /// </summary>
    Reply,

    /// <summary>
    ///     The select menu should use <see cref="Discord.WebSocket.SocketMessageComponent.UpdateAsync" />.
    /// </summary>
    Update,
}
