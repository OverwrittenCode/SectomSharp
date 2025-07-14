using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Extensions;

internal static class PropertyBuilderExtensions
{
    /// <summary>
    ///     Configures the property as a Discord Snowflake ID.
    ///     Applies a conversion from <see cref="ulong" /> to <see cref="long" /> and disables value generation.
    /// </summary>
    /// <param name="builder">The property builder for a <see cref="ulong" /> property.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder<ulong> IsRequiredSnowflakeId(this PropertyBuilder<ulong> builder) => builder.HasConversion<long>().IsRequired().ValueGeneratedNever();

    /// <summary>
    ///     Configures the property as a nullable Discord Snowflake ID.
    ///     Applies a conversion from <see cref="ulong" /> to <see cref="long" /> and disables value generation.
    /// </summary>
    /// <param name="builder">The property builder for a <see cref="ulong" /> property.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder<ulong?> IsSnowflakeId(this PropertyBuilder<ulong?> builder) => builder.HasConversion<long?>().ValueGeneratedNever();

    /// <summary>
    ///     Configures the property as a non-negative int32 that never exceeds <see cref="Int32.MaxValue" />.
    /// </summary>
    /// <param name="builder">The property builder for a <see cref="UInt32" /> property.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder<uint> IsRequiredNonNegativeInt(this PropertyBuilder<uint> builder) => builder.HasConversion<int>().IsRequired();

    /// <summary>
    ///     Configures the property as a nullable non-negative nullable int32 that never exceeds <see cref="Int32.MaxValue" />.
    /// </summary>
    /// <param name="builder">The property builder for a <see cref="UInt32" /> property.</param>
    /// <returns>The configured property builder.</returns>
    public static PropertyBuilder<uint?> IsNonNegativeInt(this PropertyBuilder<uint?> builder) => builder.HasConversion<int?>();
}
