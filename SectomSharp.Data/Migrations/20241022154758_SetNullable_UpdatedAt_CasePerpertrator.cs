using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations;

/// <inheritdoc />
public partial class SetNullable_UpdatedAt_CasePerpertrator : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Cases_Snowflake_PerpetratorId", table: "Cases");

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Snowflake",
            type: "timestamptz",
            nullable: true,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: true,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Cases",
            type: "timestamptz",
            nullable: true,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<decimal>(
            name: "PerpetratorId",
            table: "Cases",
            type: "numeric(20,0)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(20,0)"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Snowflake_PerpetratorId",
            table: "Cases",
            column: "PerpetratorId",
            principalTable: "Snowflake",
            principalColumn: "Id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Cases_Snowflake_PerpetratorId", table: "Cases");

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Snowflake",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldNullable: true,
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldNullable: true,
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Cases",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldNullable: true,
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<decimal>(
            name: "PerpetratorId",
            table: "Cases",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "numeric(20,0)",
            oldNullable: true
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Snowflake_PerpetratorId",
            table: "Cases",
            column: "PerpetratorId",
            principalTable: "Snowflake",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }
}
