using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultValuesForGuildConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Configuration",
                table: "Guilds",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Configuration",
                table: "Guilds",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb"
            );
        }
    }
}
