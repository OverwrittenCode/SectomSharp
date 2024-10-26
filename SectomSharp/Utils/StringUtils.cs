using System.Text;

namespace SectomSharp.Utils;

internal static class StringUtils
{
    private static readonly Random Random = new();
    private static readonly object LockObj = new();

    /// <summary>
    ///     Generates a unique identifier string consisting of uppercase letters and digits.
    /// </summary>
    /// <param name="length">The length of the unique identifier.</param>
    /// <returns>A unique identifier string with the given <paramref name="length" />.</returns>
    public static string GenerateUniqueId(int length = 6)
    {
        const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var stringBuilder = new StringBuilder(length);

        lock (LockObj)
        {
            for (var i = 0; i < length; i++)
            {
                stringBuilder.Append(Chars[Random.Next(Chars.Length)]);
            }
        }

        return stringBuilder.ToString();
    }

    /// <inheritdoc cref="GenerateComponentIdRegex(String, global::System.String[])" />
    /// <typeparam name="T">The class responsible for the component handling.</typeparam>
    public static string GenerateComponentIdRegex<T>(params string[] wildcardNames) =>
        GenerateComponentIdRegex(typeof(T).Name, wildcardNames);

    /// <summary>
    ///     Generates a lazy regex match for a component to match
    ///     <see cref="Discord.Interactions.ComponentInteractionAttribute.CustomId" />.
    /// </summary>
    /// <param name="prefix">The prefix of the component id.</param>
    /// <param name="wildcardNames">The names of the wildcard matches.</param>
    /// <returns>
    ///     A <see cref="Discord.Interactions.ComponentInteractionAttribute.CustomId" />
    ///     with lazy regex matching.
    /// </returns>
    /// <remarks>
    ///     See <seealso cref="GenerateComponentIdRegex(String, global::System.String[])" />
    ///     for generating the corresponding component id.
    /// </remarks>
    public static string GenerateComponentIdRegex(string prefix, params string[] wildcardNames)
    {
        const string LazyWildcardRegex = $"{Constants.ComponentWildcardSeparator}*";

        if (wildcardNames.Length == 0)
        {
            return prefix;
        }

        return prefix + String.Concat(Enumerable.Repeat(LazyWildcardRegex, wildcardNames.Length));
    }

    /// <summary>
    ///     Generates an id for a component.
    /// </summary>
    /// <param name="prefix">The prefix of the component id.</param>
    /// <param name="values">The arguments of the wildcards.</param>
    public static string GenerateComponentId(string prefix, params object[] values)
    {
        if (values.Length == 0)
        {
            return prefix;
        }

        return String.Join(
            Constants.ComponentWildcardSeparator,
            values.Prepend(prefix).Select(val => val.ToString())
        );
    }

    /// <inheritdoc cref="GenerateComponentId(String, global::System.Object[])" />
    /// <inheritdoc cref="GenerateComponentIdRegex{T}(global::System.String[])" path="/typeparam" />
    public static string GenerateComponentId<T>(params object[] values) =>
        GenerateComponentId(typeof(T).Name, values);
}
