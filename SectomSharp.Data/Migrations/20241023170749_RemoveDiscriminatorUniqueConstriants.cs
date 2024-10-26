using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations;

/// <inheritdoc />
public partial class RemoveDiscriminatorUniqueConstriants : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Snowflake_Id", table: "Snowflake");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Snowflake_Id",
            table: "Snowflake",
            column: "Id",
            unique: true
        );
    }
}
