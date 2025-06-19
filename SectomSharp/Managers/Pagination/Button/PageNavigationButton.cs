namespace SectomSharp.Managers.Pagination.Button;

/// <summary>
///     Represents a button that was clicked to navigate through a paginated collection.
/// </summary>
public enum PageNavigationButton
{
    /// <summary>
    ///     Goes to the first page.
    /// </summary>
    Start,

    /// <summary>
    ///     Goes to the previous page.
    /// </summary>
    Previous,

    /// <summary>
    ///     Goes to the next page.
    /// </summary>
    Next,

    /// <summary>
    ///     Goes the to last page.
    /// </summary>
    End,

    /// <summary>
    ///     Disposes the pagination control options.
    /// </summary>
    Exit
}
