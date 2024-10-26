using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations;

/// <inheritdoc />
public partial class FixTimestampIssue : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Snowflake",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Cases",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Snowflake",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Cases",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz"
        );
    }
}
