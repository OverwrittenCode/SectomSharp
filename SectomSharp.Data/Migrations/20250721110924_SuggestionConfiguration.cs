using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SuggestionConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Configuration_Suggestion_IsDisabled",
                table: "Guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SuggestionPanels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionPanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestionPanels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionPosts",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpvoteCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DownvoteCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionPosts", x => new { x.GuildId, x.Id });
                    table.ForeignKey(
                        name: "FK_SuggestionPosts_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionPosts_Users_GuildId_AuthorId",
                        columns: x => new { x.GuildId, x.AuthorId },
                        principalTable: "Users",
                        principalColumns: new[] { "GuildId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    PanelId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Emote = table.Column<string>(type: "character varying(57)", maxLength: 57, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestionComponents_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionComponents_SuggestionPanels_PanelId",
                        column: x => x.PanelId,
                        principalTable: "SuggestionPanels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionVotes",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SuggestionId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionVotes", x => new { x.GuildId, x.UserId, x.SuggestionId });
                    table.ForeignKey(
                        name: "FK_SuggestionVotes_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionVotes_SuggestionPosts_GuildId_SuggestionId",
                        columns: x => new { x.GuildId, x.SuggestionId },
                        principalTable: "SuggestionPosts",
                        principalColumns: new[] { "GuildId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionVotes_Users_GuildId_UserId",
                        columns: x => new { x.GuildId, x.UserId },
                        principalTable: "Users",
                        principalColumns: new[] { "GuildId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComponents_GuildId_PanelId_Name",
                table: "SuggestionComponents",
                columns: new[] { "GuildId", "PanelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionComponents_PanelId",
                table: "SuggestionComponents",
                column: "PanelId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionPanels_GuildId_Name",
                table: "SuggestionPanels",
                columns: new[] { "GuildId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionPosts_GuildId_AuthorId",
                table: "SuggestionPosts",
                columns: new[] { "GuildId", "AuthorId" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionVotes_GuildId_SuggestionId_Type",
                table: "SuggestionVotes",
                columns: new[] { "GuildId", "SuggestionId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionVotes_GuildId_UserId_SuggestionId",
                table: "SuggestionVotes",
                columns: new[] { "GuildId", "UserId", "SuggestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuggestionComponents");

            migrationBuilder.DropTable(
                name: "SuggestionVotes");

            migrationBuilder.DropTable(
                name: "SuggestionPanels");

            migrationBuilder.DropTable(
                name: "SuggestionPosts");

            migrationBuilder.DropColumn(
                name: "Configuration_Suggestion_IsDisabled",
                table: "Guilds");
        }
    }
}
