using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompositeKeysAndScopedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_TargetId",
                table: "Cases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_GuildId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_GuildId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_Id",
                table: "Guilds");

            migrationBuilder.DropIndex(
                name: "IX_Channels_GuildId",
                table: "Channels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cases",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_Id",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_TargetId",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_BotLogChannels_GuildId",
                table: "BotLogChannels");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogChannels_GuildId",
                table: "AuditLogChannels");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cases",
                table: "Cases",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_GuildId_Id",
                table: "Roles",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_GuildId_Id",
                table: "Channels",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_GuildId_PerpetratorId",
                table: "Cases",
                columns: new[] { "GuildId", "PerpetratorId" });

            migrationBuilder.CreateIndex(
                name: "IX_BotLogChannels_GuildId_Id",
                table: "BotLogChannels",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogChannels_GuildId_Id",
                table: "AuditLogChannels",
                columns: new[] { "GuildId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_GuildId_PerpetratorId",
                table: "Cases",
                columns: new[] { "GuildId", "PerpetratorId" },
                principalTable: "Users",
                principalColumns: new[] { "GuildId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_GuildId_TargetId",
                table: "Cases",
                columns: new[] { "GuildId", "TargetId" },
                principalTable: "Users",
                principalColumns: new[] { "GuildId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_GuildId_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_GuildId_TargetId",
                table: "Cases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_GuildId_Id",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Channels_GuildId_Id",
                table: "Channels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cases",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_GuildId_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_BotLogChannels_GuildId_Id",
                table: "BotLogChannels");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogChannels_GuildId_Id",
                table: "AuditLogChannels");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cases",
                table: "Cases",
                columns: new[] { "Id", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuildId",
                table: "Users",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_GuildId",
                table: "Roles",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Id",
                table: "Guilds",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_GuildId",
                table: "Channels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Id",
                table: "Cases",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_PerpetratorId",
                table: "Cases",
                column: "PerpetratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TargetId",
                table: "Cases",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_BotLogChannels_GuildId",
                table: "BotLogChannels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogChannels_GuildId",
                table: "AuditLogChannels",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_PerpetratorId",
                table: "Cases",
                column: "PerpetratorId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_TargetId",
                table: "Cases",
                column: "TargetId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
