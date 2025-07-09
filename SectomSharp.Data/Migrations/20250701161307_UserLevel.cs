using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases");

            migrationBuilder.AddColumn<int>(
                name: "Level_CurrentXp",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level_UpdatedAt",
                table: "Users",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelingRoles_GuildId_Id",
                table: "LevelingRoles",
                columns: new[] { "GuildId", "Id" })
                .Annotation("Npgsql:IndexInclude", new[] { "Cooldown", "Multiplier" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases",
                columns: new[] { "GuildId", "TargetId" },
                filter: "\"LogType\" = 1 AND \"OperationType\" = 0 ");
            
            // language=SQL
            migrationBuilder.Sql("""
                                 CREATE INDEX IF NOT EXISTS "IX_Guilds_Leveling_Enabled"
                                     ON "Guilds" ("Id")
                                     INCLUDE (
                                         "Configuration_Leveling_GlobalCooldown",
                                         "Configuration_Leveling_GlobalMultiplier",
                                         "Configuration_Leveling_AccumulateMultipliers"
                                     )
                                     WHERE NOT "Configuration_Leveling_IsDisabled";
                                 """);
            // language=SQL
            migrationBuilder.Sql("""
                                 CREATE INDEX IF NOT EXISTS "IX_Users_GuildId_Id_Level"
                                     ON "Users" ("GuildId", "Id")
                                     INCLUDE (
                                         "Level_CurrentXp",
                                         "Level_UpdatedAt"
                                     );
                                 """);

            // language=SQL
            migrationBuilder.Sql("""
                                 CREATE INDEX IF NOT EXISTS "IX_Users_GuildId_CurrentXp"
                                     ON "Users" ("GuildId", "Level_CurrentXp" DESC)
                                     INCLUDE ("Id");
                                 """);
            
            const int baseMultiplier = 45;
            const int baseOffset = 135;
            const int minXpGain = 10;
            
            // language=SQL
            migrationBuilder.Sql($"""
                                  CREATE OR REPLACE FUNCTION get_level(xp integer)
                                  RETURNS integer
                                  LANGUAGE sql
                                  IMMUTABLE
                                  AS $$
                                      SELECT FLOOR(GREATEST(0, (-{baseOffset} + SQRT({baseOffset} * {baseOffset} + 4 * {baseMultiplier} * xp)) / (2.0 * {baseMultiplier})))::INTEGER;
                                  $$;
                                  """);

            // language=SQL
            migrationBuilder.Sql($"""
                                  CREATE OR REPLACE FUNCTION get_required_xp(level integer)
                                  RETURNS integer
                                  LANGUAGE sql
                                  IMMUTABLE
                                  AS $$
                                      SELECT ({baseMultiplier} * (level + 1) * (level + 1) + {baseOffset} * (level + 1))::INTEGER;
                                  $$;
                                  """);

            // language=SQL
            migrationBuilder.Sql($"""
                                  CREATE OR REPLACE FUNCTION get_xp_gain(multiplier float)
                                  RETURNS integer
                                  LANGUAGE sql
                                  VOLATILE
                                  AS $$
                                      SELECT (FLOOR(RANDOM() * ({baseMultiplier} - {minXpGain} + 1) * multiplier) + {minXpGain})::INTEGER;
                                  $$;
                                  """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LevelingRoles_GuildId_Id",
                table: "LevelingRoles");

            migrationBuilder.DropIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Level_CurrentXp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Level_UpdatedAt",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_GuildId_TargetId_Warn_Create",
                table: "Cases",
                columns: new[] { "GuildId", "TargetId" },
                filter: "    \"LogType\" = 1 AND \"OperationType\" = 0");
            
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP INDEX IF EXISTS "IX_Guilds_Leveling_Enabled";
                                 """);
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP INDEX IF EXISTS "IX_Users_GuildId_Id_Level";
                                 """);
            
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP INDEX IF EXISTS "IX_Users_GuildId_CurrentXp";
                                 """);
            
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP FUNCTION IF EXISTS get_level(integer);
                                 """);
            
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP FUNCTION IF EXISTS get_required_xp(integer);
                                 """);
            
            // language=SQL
            migrationBuilder.Sql("""
                                 DROP FUNCTION IF EXISTS get_xp_gain(float);
                                 """);
        }
    }
}
