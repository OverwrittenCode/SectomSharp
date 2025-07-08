using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SectomSharp.Data.Entities;

namespace SectomSharp.Utils;

internal static partial class StringUtils
{
    private static ReadOnlySpan<byte> ByteSpan => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"u8;

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex HumanisePascalCase { get; }

    /// <summary>
    ///     Generates a unique identifier string consisting of uppercase letters and digits.
    /// </summary>
    /// <returns>A unique identifier string.</returns>
    public static string GenerateUniqueId()
        => String.Create(
            CaseConfiguration.IdLength,
            (byte)0,
            (span, _) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = (char)Unsafe.Add(ref Unsafe.AsRef(in ByteSpan.GetPinnableReference()), Random.Shared.Next(ByteSpan.Length));
                }
            }
        );

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
}
