using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SectomSharp.Utils;

internal static class FormattingUtils
{
    /// <summary>
    ///     Counts the number of decimal digits required to represent the specified unsigned 64-bit integer value.
    /// </summary>
    /// <param name="ignored">An ignored parameter used for signature matching with runtime intrinsic.</param>
    /// <param name="value">The unsigned 64-bit integer value whose digits are counted.</param>
    /// <returns>The number of decimal digits in <paramref name="value" />.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(CountDigits))]
    [Pure]
    public extern static int CountDigits([UnsafeAccessorType("System.Buffers.Text.FormattingHelpers, System.Private.CoreLib")] object? ignored, ulong value);

    /// <summary>
    ///     Counts the number of decimal digits required to represent the specified unsigned 32-bit integer value.
    /// </summary>
    /// <param name="ignored">An ignored parameter used for signature matching with runtime intrinsic.</param>
    /// <param name="value">The unsigned 32-bit integer value whose digits are counted.</param>
    /// <returns>The number of decimal digits in <paramref name="value" />.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(CountDigits))]
    [Pure]
    public extern static int CountDigits([UnsafeAccessorType("System.Buffers.Text.FormattingHelpers, System.Private.CoreLib")] object? ignored, uint value);
}
