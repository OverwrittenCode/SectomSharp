using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Discord;
using SectomSharp.Utils;

namespace SectomSharp.Extensions;

internal static class DiscordExtensions
{
    /// <summary>
    ///     Asynchronously responds or follows up a module based on <see cref="IDiscordInteraction.HasResponded" />.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of responding or following up the interaction.</returns>
    /// <inheritdoc cref="IDiscordInteraction.RespondAsync" />
    public static async Task RespondOrFollowupAsync(
        this IDiscordInteraction interaction,
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    )
    {
        if (interaction.HasResponded)
        {
            await interaction.FollowupAsync(text, embeds, false, ephemeral, allowedMentions, components, null, options, poll);
        }
        else
        {
            await interaction.RespondAsync(text, embeds, false, ephemeral, allowedMentions, components, null, options, poll);
        }
    }

    /// <summary>
    ///     Converts a Discord <see cref="Color" /> to a hyperlinked
    ///     URL that displays its hex code.
    /// </summary>
    /// <returns>The URL.</returns>
    [SkipLocalsInit]
    public static string ToHyperlinkedColourPicker(this Color color)
    {
        const string prefix = "[#";
        const string middleFix = "](https://imagecolorpicker.com/color-code/";
        const int hexLength = 6;
        const char suffix = ')';

        int totalLength = prefix.Length + hexLength + middleFix.Length + hexLength + 1;
        string buffer = StringUtils.FastAllocateString(null, totalLength);
        ref char start = ref StringUtils.GetFirstChar(buffer);
        ref char current = ref start;
        current = ref StringUtils.CopyTo(ref current, prefix);

        ref char hexStart = ref current;

        // Write 6 hex digits of the color as uppercase characters.
        //
        // If AVX2, SSSE3, and SSE2 intrinsics are supported, use SIMD to convert all 6 nibbles
        // in one go, storing 8 UTF-16 chars (16 bytes) at once. Only the first 6 chars contain
        // valid hex digits; the last 2 chars are overwritten but unused, so the buffer must
        // have space for at least 8 chars to avoid memory corruption.
        //
        // Otherwise, fall back to a simple loop converting each nibble to hex manually.
        //
        // In either case, advance the pointer by 6 chars after writing.
        if (Avx2.IsSupported && Ssse3.IsSupported && Sse2.IsSupported)
        {
            Vector128<int> v = Sse2.Shuffle(Sse2.ConvertScalarToVector128Int32((int)color.RawValue), 0b00_00_00_00);

            var shifts1 = Vector128.Create(20U, 16, 12, 8);
            var shifts2 = Vector128.Create(4U, 0, 0, 0);

            Vector128<int> nibs0To3 = Avx2.ShiftRightLogicalVariable(v, shifts1);
            nibs0To3 = Sse2.And(nibs0To3, Vector128.Create(0xF));

            Vector128<int> nibs4To5 = Avx2.ShiftRightLogicalVariable(v, shifts2);
            nibs4To5 = Sse2.And(nibs4To5, Vector128.Create(0xF));

            Vector128<short> packed16 = Sse2.PackSignedSaturate(nibs0To3, nibs4To5);
            Vector128<byte> packed8 = Sse2.PackUnsignedSaturate(packed16, packed16);

            var lookupTable = Vector128.Create(
                (byte)'0',
                (byte)'1',
                (byte)'2',
                (byte)'3',
                (byte)'4',
                (byte)'5',
                (byte)'6',
                (byte)'7',
                (byte)'8',
                (byte)'9',
                (byte)'A',
                (byte)'B',
                (byte)'C',
                (byte)'D',
                (byte)'E',
                (byte)'F'
            );

            Vector128<byte> asciiBytes = Ssse3.Shuffle(lookupTable, packed8);
            Vector128<ushort> utf16Chars = Sse2.UnpackLow(asciiBytes, Vector128<byte>.Zero).AsUInt16();

            // Store 8 UTF-16 chars (16 bytes) because Sse2.Store writes the full 128-bit vector.
            // Only the first 6 chars contain valid hex digits; the last 2 chars are overwritten but unused.
            // The buffer must have space for at least 8 chars to avoid memory corruption.
            utf16Chars.StoreUnsafe(ref Unsafe.As<char, ushort>(ref current));
        }
        else
        {
            ref char reference = ref StringUtils.GetFirstChar("0123456789ABCDEF");
            uint val = color.RawValue;

            Unsafe.Add(ref current, 0) = Unsafe.Add(ref reference, (int)((val >> 20) & 0xF));
            Unsafe.Add(ref current, 1) = Unsafe.Add(ref reference, (int)((val >> 16) & 0xF));
            Unsafe.Add(ref current, 2) = Unsafe.Add(ref reference, (int)((val >> 12) & 0xF));
            Unsafe.Add(ref current, 3) = Unsafe.Add(ref reference, (int)((val >> 8) & 0xF));
            Unsafe.Add(ref current, 4) = Unsafe.Add(ref reference, (int)((val >> 4) & 0xF));
            Unsafe.Add(ref current, 5) = Unsafe.Add(ref reference, (int)((val >> 0) & 0xF));
        }

        current = ref StringUtils.CopyTo(ref Unsafe.Add(ref current, hexLength), middleFix);
        current = ref StringUtils.CopyTo(ref current, ref hexStart, hexLength);
        current = suffix;
        current = ref Unsafe.Add(ref current, 1);

        ref char end = ref Unsafe.Add(ref start, totalLength);
        Debug.Assert(Unsafe.AreSame(ref current, ref end));

        return buffer;
    }
}
