using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SectomSharp.Data.Enums;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDiscriminatorToOwnTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Snowflake_ChannelId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Snowflake_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Snowflake_TargetId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Snowflake_Guilds_GuildId",
                table: "Snowflake");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Snowflake",
                table: "Snowflake");

            migrationBuilder.DropColumn(
                name: "AuditLogType",
                table: "Snowflake");

            migrationBuilder.DropColumn(
                name: "BotLogType",
                table: "Snowflake");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Snowflake");

            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Snowflake");

            migrationBuilder.RenameTable(
                name: "Snowflake",
                newName: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_Snowflake_GuildId",
                table: "Users",
                newName: "IX_Users_GuildId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AuditLogChannels",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WebhookUrl = table.Column<string>(type: "text", nullable: false),
                    AuditLogType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogChannels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotLogChannels",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BotLogType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotLogChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotLogChannels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogChannels_GuildId",
                table: "AuditLogChannels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_BotLogChannels_GuildId",
                table: "BotLogChannels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_GuildId",
                table: "Channels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_GuildId",
                table: "Roles",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Channels_ChannelId",
                table: "Cases",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guilds_GuildId",
                table: "Users",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Channels_ChannelId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_PerpetratorId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_TargetId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guilds_GuildId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AuditLogChannels");

            migrationBuilder.DropTable(
                name: "BotLogChannels");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Snowflake");

            migrationBuilder.RenameIndex(
                name: "IX_Users_GuildId",
                table: "Snowflake",
                newName: "IX_Snowflake_GuildId");

            migrationBuilder.AddColumn<int>(
                name: "AuditLogType",
                table: "Snowflake",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BotLogType",
                table: "Snowflake",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<SnowflakeType>(
                name: "Type",
                table: "Snowflake",
                type: "snowflake_type",
                nullable: false,
                defaultValue: SnowflakeType.None);

            migrationBuilder.AddColumn<string>(
                name: "WebhookUrl",
                table: "Snowflake",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Snowflake",
                table: "Snowflake",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Snowflake_ChannelId",
                table: "Cases",
                column: "ChannelId",
                principalTable: "Snowflake",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Snowflake_PerpetratorId",
                table: "Cases",
                column: "PerpetratorId",
                principalTable: "Snowflake",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Snowflake_TargetId",
                table: "Cases",
                column: "TargetId",
                principalTable: "Snowflake",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Snowflake_Guilds_GuildId",
                table: "Snowflake",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
