using System.Text;
using System.Text.RegularExpressions;
using SectomSharp.Data.Configurations;

namespace SectomSharp.Utils;

internal static partial class StringUtils
{
    private static readonly Random Random = new();
    private static readonly Lock Lock = new();

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex HumanisePascalCase { get; }

    /// <summary>
    ///     Generates a unique identifier string consisting of uppercase letters and digits.
    /// </summary>
    /// <returns>A unique identifier string.</returns>
    public static string GenerateUniqueId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var stringBuilder = new StringBuilder(CaseConfiguration.IdLength);

        lock (Lock)
        {
            for (int i = 0; i < CaseConfiguration.IdLength; i++)
            {
                stringBuilder.Append(chars[Random.Next(chars.Length)]);
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Transforms string with PascalCase by adding a whitespace gap between each word.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The transformed string.</returns>
    public static string PascalCaseToSentenceCase(string input) => HumanisePascalCase.Replace(input, " ");

    /// <summary>
    ///     Transforms string with PascalCase by adding a hyphen between each word.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The transformed string.</returns>
    public static string PascalCaseToKebabCase(string input) => HumanisePascalCase.Replace(input, "-").ToLower();

    /// <inheritdoc cref="GenerateComponentIdRegex(String, global::System.String[])" />
    /// <typeparam name="T">The class responsible for the component handling.</typeparam>
    public static string GenerateComponentIdRegex<T>(params string[] wildcardNames) => GenerateComponentIdRegex(typeof(T).Name, wildcardNames);

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
        const string lazyWildcardRegex = $"{Constants.ComponentWildcardSeparator}*";

        if (wildcardNames.Length == 0)
        {
            return prefix;
        }

        return prefix + String.Concat(Enumerable.Repeat(lazyWildcardRegex, wildcardNames.Length));
    }

    /// <summary>
    ///     Generates an id for a component.
    /// </summary>
    /// <param name="prefix">The prefix of the component id.</param>
    /// <param name="values">The arguments of the wildcards.</param>
    public static string GenerateComponentId(string prefix, params object[] values)
        => values.Length == 0 ? prefix : String.Join(Constants.ComponentWildcardSeparator, values.Prepend(prefix).Select(val => val.ToString()));

    /// <inheritdoc cref="GenerateComponentId(String, global::System.Object[])" />
    /// <inheritdoc cref="GenerateComponentIdRegex{T}(global::System.String[])" path="/typeparam" />
    public static string GenerateComponentId<T>(params object[] values) => GenerateComponentId(typeof(T).Name, values);
}
