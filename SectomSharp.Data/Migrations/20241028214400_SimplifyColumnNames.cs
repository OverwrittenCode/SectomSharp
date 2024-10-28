using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BotLogType",
                table: "BotLogChannels",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "AuditLogType",
                table: "AuditLogChannels",
                newName: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "BotLogChannels",
                newName: "BotLogType");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "AuditLogChannels",
                newName: "AuditLogType");
        }
    }
}
