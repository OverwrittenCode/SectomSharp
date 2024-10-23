using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyForConvienence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Cases_Snowflake_ChannelId", table: "Cases");

            migrationBuilder.DropColumn(name: "LogMessageId", table: "Cases");

            migrationBuilder.AlterColumn<decimal>(
                name: "ChannelId",
                table: "Cases",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)"
            );

            migrationBuilder.AddColumn<string>(
                name: "LogMessageURL",
                table: "Cases",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Snowflake_ChannelId",
                table: "Cases",
                column: "ChannelId",
                principalTable: "Snowflake",
                principalColumn: "Id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Cases_Snowflake_ChannelId", table: "Cases");

            migrationBuilder.DropColumn(name: "LogMessageURL", table: "Cases");

            migrationBuilder.AlterColumn<decimal>(
                name: "ChannelId",
                table: "Cases",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true
            );

            migrationBuilder.AddColumn<decimal>(
                name: "LogMessageId",
                table: "Cases",
                type: "numeric(20,0)",
                nullable: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Snowflake_ChannelId",
                table: "Cases",
                column: "ChannelId",
                principalTable: "Snowflake",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
