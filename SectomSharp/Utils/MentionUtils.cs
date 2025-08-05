using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace SectomSharp.Utils;

[PublicAPI]
internal static class MentionUtils
{
    public const string ChannelMentionStart = "<#";
    public const string RoleMentionStart = "<@&";
    public const string UserMentionStart = "<@";
    public const char MentionEnd = '>';
    public const int SnowflakeIdMaxLength = 20;
    public const int ChannelMentionMaxLength = SnowflakeIdMaxLength + 3 + 1;
    public const int UserMentionMaxLength = SnowflakeIdMaxLength + 3 + 1;
    public const int RoleMentionMaxLength = SnowflakeIdMaxLength + 4 + 1;

    /// <summary>
    ///     Writes a snowflake id to the destination pointer and advances the destination pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="value">The snowflake id.</param>
    /// <param name="digitCount">The number of decimal digits in the snowflake id.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    /// <seealso cref="FormattingUtils.CountDigits(System.Object,System.UInt64)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char WriteSnowflakeId(ref char destination, ulong value, int digitCount)
    {
        switch (digitCount)
        {
            case 17:
                {
                    ref char end = ref Unsafe.Add(ref destination, 17);
                    ref char bufferEnd = ref end;

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    Unsafe.Subtract(ref bufferEnd, 1) = (char)('0' + value);

                    return ref end;
                }

            case 18:
                {
                    ref char end = ref Unsafe.Add(ref destination, 18);
                    ref char bufferEnd = ref end;

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    _ = WriteTwoDigits(ref bufferEnd, ref value);

                    return ref end;
                }

            case 19:
                {
                    ref char end = ref Unsafe.Add(ref destination, 19);
                    ref char bufferEnd = ref end;

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    Unsafe.Subtract(ref bufferEnd, 1) = (char)('0' + value);

                    return ref end;
                }

            case 20:
                {
                    ref char end = ref Unsafe.Add(ref destination, 20);
                    ref char bufferEnd = ref end;

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);

                    bufferEnd = ref WriteTwoDigits(ref bufferEnd, ref value);
                    _ = WriteTwoDigits(ref bufferEnd, ref value);

                    return ref end;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(digitCount), digitCount, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
        static ref char WriteTwoDigits(ref char bufferEnd, scoped ref ulong value)
        {
            ReadOnlySpan<byte> twoDigitsCharsAsBytes = "0\00\00\01\00\02\00\03\00\04\00\05\00\06\00\07\00\08\00\09\0"u8
                                                     + "1\00\01\01\01\02\01\03\01\04\01\05\01\06\01\07\01\08\01\09\0"u8
                                                     + "2\00\02\01\02\02\02\03\02\04\02\05\02\06\02\07\02\08\02\09\0"u8
                                                     + "3\00\03\01\03\02\03\03\03\04\03\05\03\06\03\07\03\08\03\09\0"u8
                                                     + "4\00\04\01\04\02\04\03\04\04\04\05\04\06\04\07\04\08\04\09\0"u8
                                                     + "5\00\05\01\05\02\05\03\05\04\05\05\05\06\05\07\05\08\05\09\0"u8
                                                     + "6\00\06\01\06\02\06\03\06\04\06\05\06\06\06\07\06\08\06\09\0"u8
                                                     + "7\00\07\01\07\02\07\03\07\04\07\05\07\06\07\07\07\08\07\09\0"u8
                                                     + "8\00\08\01\08\02\08\03\08\04\08\05\08\06\08\07\08\08\08\09\0"u8
                                                     + "9\00\09\01\09\02\09\03\09\04\09\05\09\06\09\07\09\08\09\09\0"u8;

            bufferEnd = ref Unsafe.Subtract(ref bufferEnd, 2);
            (value, ulong remainder) = Math.DivRem(value, 100);
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<char, byte>(ref bufferEnd),
                ref Unsafe.Add(ref MemoryMarshal.GetReference(twoDigitsCharsAsBytes), (uint)sizeof(char) * 2 * (uint)remainder),
                (uint)sizeof(char) * 2
            );

            return ref bufferEnd;
        }
    }

    /// <summary>
    ///     Writes a user mention string to the given destination managed pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="id">The user ID.</param>
    /// <param name="digitCount">The number of decimal digits in the user ID.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    /// <seealso cref="FormattingUtils.CountDigits(System.Object,System.UInt64)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char WriteMentionUser(ref char destination, ulong id, int digitCount)
    {
        destination = ref StringUtils.CopyTo(ref destination, UserMentionStart);
        destination = ref WriteSnowflakeId(ref destination, id, digitCount);
        destination = MentionEnd;
        destination = ref Unsafe.Add(ref destination, 1);
        return ref destination;
    }

    /// <summary>
    ///     Returns a mention string based on the user ID.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>
    ///     A user mention string (e.g. &lt;@80351110224678912&gt;).
    /// </returns>
    [SkipLocalsInit]
    [Pure]
    public static string MentionUser(ulong id)
    {
        int digitCount = FormattingUtils.CountDigits(null, id);
        int totalLength = UserMentionStart.Length + digitCount + 1;

        string buffer = StringUtils.FastAllocateString(null, totalLength);
        ref char start = ref StringUtils.GetFirstChar(buffer);
        ref char current = ref start;
        current = ref WriteMentionRole(ref current, id, digitCount);

        ref char end = ref Unsafe.Add(ref start, totalLength);
        Debug.Assert(Unsafe.AreSame(ref current, ref end));

        return buffer;
    }

    /// <summary>
    ///     Writes a role mention string to the given destination managed pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="id">The role ID.</param>
    /// <param name="digitCount">The number of decimal digits in the role ID.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    /// <seealso cref="FormattingUtils.CountDigits(System.Object,System.UInt64)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char WriteMentionRole(ref char destination, ulong id, int digitCount)
    {
        destination = ref StringUtils.CopyTo(ref destination, RoleMentionStart);
        destination = ref WriteSnowflakeId(ref destination, id, digitCount);
        destination = MentionEnd;
        destination = ref Unsafe.Add(ref destination, 1);
        return ref destination;
    }

    /// <summary>
    ///     Returns a mention string based on the role ID.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>
    ///     A role mention string (e.g. &lt;@&amp;165511591545143296&gt;).
    /// </returns>
    [SkipLocalsInit]
    [Pure]
    public static string MentionRole(ulong id)
    {
        int digitCount = FormattingUtils.CountDigits(null, id);
        int totalLength = RoleMentionStart.Length + digitCount + 1;

        string buffer = StringUtils.FastAllocateString(null, totalLength);
        ref char start = ref StringUtils.GetFirstChar(buffer);
        ref char current = ref start;
        current = ref WriteMentionRole(ref current, id, digitCount);

        ref char end = ref Unsafe.Add(ref start, totalLength);
        Debug.Assert(Unsafe.AreSame(ref current, ref end));

        return buffer;
    }

    /// <summary>
    ///     Writes a channel mention string to the managed destination pointer.
    /// </summary>
    /// <param name="destination">The managed pointer to write into.</param>
    /// <param name="id">The channel ID.</param>
    /// <param name="digitCount">The number of decimal digits in the channel ID.</param>
    /// <returns>A managed pointer to the position immediately after the written characters.</returns>
    /// <seealso cref="FormattingUtils.CountDigits(System.Object,System.UInt64)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    [MustUseReturnValue("Return value advances the managed destination pointer; use it when performing subsequent writes.")]
    public static ref char WriteMentionChannel(ref char destination, ulong id, int digitCount)
    {
        destination = ref StringUtils.CopyTo(ref destination, ChannelMentionStart);
        destination = ref WriteSnowflakeId(ref destination, id, digitCount);
        destination = MentionEnd;
        destination = ref Unsafe.Add(ref destination, 1);
        return ref destination;
    }

    /// <summary>
    ///     Returns a mention string based on the channel ID.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>
    ///     A channel mention string (e.g. &lt;#103735883630395392&gt;).
    /// </returns>
    [SkipLocalsInit]
    [Pure]
    public static string MentionChannel(ulong id)
    {
        int digitCount = FormattingUtils.CountDigits(null, id);
        int totalLength = ChannelMentionStart.Length + digitCount + 1;

        string buffer = StringUtils.FastAllocateString(null, totalLength);
        ref char start = ref StringUtils.GetFirstChar(buffer);
        ref char current = ref start;
        current = ref WriteMentionChannel(ref current, id, digitCount);

        ref char end = ref Unsafe.Add(ref start, totalLength);
        Debug.Assert(Unsafe.AreSame(ref current, ref end));

        return buffer;
    }
}
