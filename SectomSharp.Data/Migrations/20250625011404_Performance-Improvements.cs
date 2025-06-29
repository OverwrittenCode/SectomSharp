using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_GuildId_TargetId_LogType_OperationType",
                table: "Cases");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:operation_type", "create,update,delete");

            migrationBuilder.AlterColumn<int>(
                name: "OperationType",
                table: "Cases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "operation_type");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases",
                columns: new[] { "GuildId", "TargetId" },
                filter: "    \"LogType\" = 1 AND \"OperationType\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:operation_type", "create,update,delete");

            migrationBuilder.AlterColumn<int>(
                name: "OperationType",
                table: "Cases",
                type: "operation_type",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_GuildId_TargetId_LogType_OperationType",
                table: "Cases",
                columns: new[] { "GuildId", "TargetId", "LogType", "OperationType" });
        }
    }
}
