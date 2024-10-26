namespace SectomSharp.Extensions;

internal static class StringExtensions
{
    /// <summary>
    ///     Truncates a string up to a given length.
    /// </summary>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>A string of maximum length <paramref name="maxLength" />.</returns>
    public static string Truncate(this string value, int maxLength) =>
        value.Length < maxLength ? value : value[..maxLength];
}
