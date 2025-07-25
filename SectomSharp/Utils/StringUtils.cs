using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SectomSharp.Data.Entities;

namespace SectomSharp.Utils;

internal static partial class StringUtils
{
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex HumanisePascalCase { get; }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(Memmove))]
    private extern static void Memmove<T>([UnsafeAccessorType("System.Buffer, System.Private.CoreLib")] object? ignored, ref T destination, ref T source, nuint elementCount);

    /// <summary>
    ///     Generates a unique identifier string consisting of uppercase letters and digits.
    /// </summary>
    /// <returns>A unique identifier string.</returns>
    [SkipLocalsInit]
    [Pure]
    public static string GenerateUniqueId()
    {
        string buffer = FastAllocateString(null, CaseConfiguration.IdLength);
        ref char start = ref GetFirstChar(buffer);
        ref char current = ref start;

        const string idContents = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        ref char reference = ref GetFirstChar(idContents);
        for (int i = 0; i < CaseConfiguration.IdLength; i++)
        {
            current = Unsafe.Add(ref reference, Random.Shared.Next(idContents.Length));
            current = ref Unsafe.Add(ref current, 1);
        }

        ref char end = ref Unsafe.Add(ref start, CaseConfiguration.IdLength);
        Debug.Assert(Unsafe.AreSame(ref current, ref end));

        return buffer;
    }

    /// <summary>
    ///     Transforms string with PascalCase by adding a whitespace gap between each word.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The transformed string.</returns>
    [Pure]
    public static string PascalCaseToSentenceCase(string input) => HumanisePascalCase.Replace(input, " ");

    /// <summary>
    ///     Transforms string with PascalCase by adding a hyphen between each word.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The transformed string.</returns>
    [Pure]
    public static string PascalCaseToKebabCase(string input) => HumanisePascalCase.Replace(input, "-").ToLower();

    /// <summary>
    ///     Allocates a new string instance of the specified length without initializing its contents.
    /// </summary>
    /// <param name="ignored">An ignored parameter used for signature matching with runtime intrinsic.</param>
    /// <param name="length">The length of the string to allocate.</param>
    /// <returns>A new string instance with uninitialized character buffer of the specified length.</returns>
    /// <remarks>
    ///     It is the caller's responsibility to <b>fully initialize every character position</b> in the returned string before the string is observed or returned from the method.
    ///     <para />
    ///     Failing to do so may result in undefined behavior or security risks, as the string's contents may contain arbitrary memory data.
    /// </remarks>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(FastAllocateString))]
    [MustUseReturnValue("Ignoring the return value would discard the allocated buffer and likely cause errors or memory leaks.")]
    public extern static string FastAllocateString([UnsafeAccessorType("System.String, System.Private.CoreLib")] object? ignored, int length);

    /// <summary>
    ///     Returns a reference to the first character of the specified string's internal buffer.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <returns>
    ///     A reference to the first character of the string.
    /// </returns>
    /// <remarks>
    ///     This method is intended to be used together with <see cref="FastAllocateString" /> to efficiently write directly into the string's character buffer
    ///     without requiring <c>fixed</c> statements or unsafe pointer pinning.
    ///     <para />
    ///     The caller must ensure that the string remains alive and unmodified for the duration of usage of this reference.
    ///     Using this reference after the string is collected or replaced results in undefined behavior.
    /// </remarks>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_firstChar")]
    [Pure]
    public extern static ref char GetFirstChar(string str);

    /// <summary>
    ///     Copies the contents of the given source string into the given destination managed pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write to.</param>
    /// <param name="source">The source string to copy from.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char CopyTo(ref char destination, string source)
    {
        Memmove(null, ref destination, ref GetFirstChar(source), (uint)source.Length);
        return ref Unsafe.Add(ref destination, source.Length);
    }

    /// <summary>
    ///     Copies the contents of the given source string into the given destination managed pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write to.</param>
    /// <param name="source">The managed pointer to copy from.</param>
    /// <param name="length">The number of characters to copy.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char CopyTo(ref char destination, scoped ref char source, uint length)
    {
        Memmove(null, ref destination, ref source, length);
        return ref Unsafe.Add(ref destination, length);
    }

    /// <summary>
    ///     Writes two fixed characters into the given destination managed pointer as a single <see cref="uint" /> write.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="c0">The first character to write.</param>
    /// <param name="c1">The second character to write.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char WriteFixed2(ref char destination, char c0, char c1)
    {
        uint value = BitConverter.IsLittleEndian ? c0 | ((uint)c1 << 16) : ((uint)c0 << 16) | c1;
        Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref destination), value);
        return ref Unsafe.Add(ref destination, 2);
    }

    /// <summary>
    ///     Writes four fixed characters into the given destination managed pointer as a single <see cref="ulong" /> write.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="c0">The first character to write.</param>
    /// <param name="c1">The second character to write.</param>
    /// <param name="c2">The third character to write.</param>
    /// <param name="c3">The fourth character to write.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue]
    public static ref char WriteFixed4(ref char destination, char c0, char c1, char c2, char c3)
    {
        ulong value = BitConverter.IsLittleEndian ? c0 | ((ulong)c1 << 16) | ((ulong)c2 << 32) | ((ulong)c3 << 48) : ((ulong)c0 << 48) | ((ulong)c1 << 32) | ((ulong)c2 << 16) | c3;
        Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref destination), value);
        return ref Unsafe.Add(ref destination, 4);
    }
}
