using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations;

/// <inheritdoc />
public partial class ModifyGuildToInheritFromBaseEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Users",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestampz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Users",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestampz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Guilds",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
        );

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "Guilds",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreatedAt", table: "Guilds");

        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Guilds");

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Users",
            type: "timestampz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Users",
            type: "timestampz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );
    }
}
