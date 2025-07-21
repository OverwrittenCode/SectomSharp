using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SectomSharp.Data.Entities;

namespace SectomSharp.Utils;

internal static partial class StringUtils
{
    private static ReadOnlySpan<byte> ByteSpan => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"u8;

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex HumanisePascalCase { get; }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(Memmove))]
    private extern static void Memmove<T>([UnsafeAccessorType("System.Buffer, System.Private.CoreLib")] object? ignored, ref T destination, ref T source, nuint elementCount);

    /// <summary>
    ///     Generates a unique identifier string consisting of uppercase letters and digits.
    /// </summary>
    /// <returns>A unique identifier string.</returns>
    [SkipLocalsInit]
    public static unsafe string GenerateUniqueId()
    {
        string buffer = FastAllocateString(null, CaseConfiguration.IdLength);
        fixed (char* bufferPtr = buffer)
        {
            char* ptr = bufferPtr;
            for (int i = 0; i < CaseConfiguration.IdLength; i++)
            {
                *ptr++ = (char)Unsafe.Add(ref MemoryMarshal.GetReference(ByteSpan), Random.Shared.Next(ByteSpan.Length));
            }

            Debug.Assert(ptr == bufferPtr + CaseConfiguration.IdLength);
        }

        return buffer;
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
    ///     Copies the contents of the given source string into the destination pointer and advances the <paramref name="destination" /> pointer.
    /// </summary>
    /// <param name="destination">Reference to the destination pointer to copy into.</param>
    /// <param name="source">The source string to copy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyTo(ref char* destination, string source)
    {
        Memmove(null, ref *destination, ref Unsafe.AsRef(in source.GetPinnableReference()), (uint)source.Length);
        destination += source.Length;
    }

    /// <summary>
    ///     Copies a sequence of characters from the source pointer into the destination pointer and advances the <paramref name="destination" /> pointer.
    /// </summary>
    /// <param name="destination">Reference to the destination pointer to copy into.</param>
    /// <param name="source">Pointer to the source characters to copy.</param>
    /// <param name="length">The number of characters to copy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyTo(ref char* destination, char* source, uint length)
    {
        Memmove(null, ref *destination, ref *source, length);
        destination += length;
    }

    /// <summary>
    ///     Writes two fixed characters to the destination pointer as a single <see cref="UInt32" /> write and advances the <paramref name="destination" /> pointer.
    /// </summary>
    /// <param name="destination">Reference to the destination pointer to write into.</param>
    /// <param name="c0">The first character to write.</param>
    /// <param name="c1">The second character to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static unsafe void WriteFixed2(ref char* destination, char c0, char c1)
    {
        *(uint*)destination = BitConverter.IsLittleEndian ? c0 | ((uint)c1 << 16) : ((uint)c0 << 16) | c1;
        destination += 2;
    }

    /// <summary>
    ///     Writes four fixed characters to the destination pointer as a single <see cref="UInt64" /> write and advances the <paramref name="destination" /> pointer.
    /// </summary>
    /// <param name="destination">Reference to the destination pointer to write into.</param>
    /// <param name="c0">The first character to write.</param>
    /// <param name="c1">The second character to write.</param>
    /// <param name="c2">The third character to write.</param>
    /// <param name="c3">The fourth character to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static unsafe void WriteFixed4(ref char* destination, char c0, char c1, char c2, char c3)
    {
        (*(ulong*)destination) = BitConverter.IsLittleEndian
            ? c0 | ((ulong)c1 << 16) | ((ulong)c2 << 32) | ((ulong)c3 << 48)
            : ((ulong)c0 << 48) | ((ulong)c1 << 32) | ((ulong)c2 << 16) | c3;
        destination += 4;
    }
}
