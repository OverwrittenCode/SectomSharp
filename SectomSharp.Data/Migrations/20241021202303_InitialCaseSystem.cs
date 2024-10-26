using Microsoft.EntityFrameworkCore.Migrations;
using SectomSharp.Data.Enums;

#nullable disable

namespace SectomSharp.Data.Migrations;

/// <inheritdoc />
public partial class InitialCaseSystem : Migration
{
    private static readonly string[] columns = new[]
    {
        "GuildId",
        "TargetId",
        "LogType",
        "OperationType",
    };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Users_Guilds_GuildId", table: "Users");

        migrationBuilder.DropPrimaryKey(name: "PK_Users", table: "Users");

        migrationBuilder.RenameTable(name: "Users", newName: "Snowflake");

        migrationBuilder.RenameIndex(
            name: "IX_Users_GuildId",
            table: "Snowflake",
            newName: "IX_Snowflake_GuildId"
        );

        migrationBuilder
            .AlterDatabase()
            .Annotation(
                "Npgsql:Enum:audit_log_type",
                "server,member,message,emoji,sticker,channel,thread,role"
            )
            .Annotation("Npgsql:Enum:bot_log_type", "warn,ban,softban,timeout,configuration")
            .Annotation("Npgsql:Enum:operation_type", "create,update,delete")
            .Annotation(
                "Npgsql:Enum:snowflake_type",
                "none,user,role,channel,bot_log_channel,audit_log_channel"
            );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Guilds",
            type: "timestamptz",
            nullable: false,
            defaultValueSql: "now()",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone"
        );

        migrationBuilder.AddColumn<string>(
            name: "Configuration",
            table: "Guilds",
            type: "jsonb",
            nullable: true
        );

        migrationBuilder.AddColumn<OperationType>(
            name: "AuditLogChannel_OperationType",
            table: "Snowflake",
            type: "operation_type",
            nullable: true
        );

        migrationBuilder.AddColumn<AuditLogType>(
            name: "AuditLogType",
            table: "Snowflake",
            type: "audit_log_type",
            nullable: true
        );

        migrationBuilder.AddColumn<BotLogType>(
            name: "BotLogType",
            table: "Snowflake",
            type: "bot_log_type",
            nullable: true
        );

        migrationBuilder.AddColumn<OperationType>(
            name: "OperationType",
            table: "Snowflake",
            type: "operation_type",
            nullable: true
        );

        migrationBuilder.AddColumn<SnowflakeType>(
            name: "Type",
            table: "Snowflake",
            type: "snowflake_type",
            nullable: false,
            defaultValue: SnowflakeType.None
        );

        migrationBuilder.AddColumn<string>(
            name: "WebhookUrl",
            table: "Snowflake",
            type: "text",
            nullable: true
        );

        migrationBuilder.AddPrimaryKey(name: "PK_Snowflake", table: "Snowflake", column: "Id");

        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                Id = table.Column<string>(type: "text", nullable: false),
                PerpetratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TargetId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                LogType = table.Column<BotLogType>(type: "bot_log_type", nullable: false),
                OperationType = table.Column<OperationType>(
                    type: "operation_type",
                    nullable: false
                ),
                ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                Reason = table.Column<string>(type: "text", nullable: true),
                LogMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                CreatedAt = table.Column<DateTime>(
                    type: "timestamptz",
                    nullable: false,
                    defaultValueSql: "now()"
                ),
                UpdatedAt = table.Column<DateTime>(
                    type: "timestamptz",
                    nullable: false,
                    defaultValueSql: "now()"
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => new { x.Id, x.GuildId });
                table.ForeignKey(
                    name: "FK_Cases_Guilds_GuildId",
                    column: x => x.GuildId,
                    principalTable: "Guilds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_Cases_Snowflake_ChannelId",
                    column: x => x.ChannelId,
                    principalTable: "Snowflake",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_Cases_Snowflake_PerpetratorId",
                    column: x => x.PerpetratorId,
                    principalTable: "Snowflake",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_Cases_Snowflake_TargetId",
                    column: x => x.TargetId,
                    principalTable: "Snowflake",
                    principalColumn: "Id"
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_Guilds_Id",
            table: "Guilds",
            column: "Id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Snowflake_Id",
            table: "Snowflake",
            column: "Id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Cases_ChannelId",
            table: "Cases",
            column: "ChannelId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Cases_GuildId_TargetId_LogType_OperationType",
            table: "Cases",
            columns: columns
        );

        migrationBuilder.CreateIndex(
            name: "IX_Cases_Id",
            table: "Cases",
            column: "Id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Cases_PerpetratorId",
            table: "Cases",
            column: "PerpetratorId"
        );

        migrationBuilder.CreateIndex(name: "IX_Cases_TargetId", table: "Cases", column: "TargetId");

        migrationBuilder.AddForeignKey(
            name: "FK_Snowflake_Guilds_GuildId",
            table: "Snowflake",
            column: "GuildId",
            principalTable: "Guilds",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Snowflake_Guilds_GuildId", table: "Snowflake");

        migrationBuilder.DropTable(name: "Cases");

        migrationBuilder.DropIndex(name: "IX_Guilds_Id", table: "Guilds");

        migrationBuilder.DropPrimaryKey(name: "PK_Snowflake", table: "Snowflake");

        migrationBuilder.DropIndex(name: "IX_Snowflake_Id", table: "Snowflake");

        migrationBuilder.DropColumn(name: "Configuration", table: "Guilds");

        migrationBuilder.DropColumn(name: "AuditLogChannel_OperationType", table: "Snowflake");

        migrationBuilder.DropColumn(name: "AuditLogType", table: "Snowflake");

        migrationBuilder.DropColumn(name: "BotLogType", table: "Snowflake");

        migrationBuilder.DropColumn(name: "OperationType", table: "Snowflake");

        migrationBuilder.DropColumn(name: "Type", table: "Snowflake");

        migrationBuilder.DropColumn(name: "WebhookUrl", table: "Snowflake");

        migrationBuilder.RenameTable(name: "Snowflake", newName: "Users");

        migrationBuilder.RenameIndex(
            name: "IX_Snowflake_GuildId",
            table: "Users",
            newName: "IX_Users_GuildId"
        );

        migrationBuilder
            .AlterDatabase()
            .OldAnnotation(
                "Npgsql:Enum:audit_log_type",
                "server,member,message,emoji,sticker,channel,thread,role"
            )
            .OldAnnotation("Npgsql:Enum:bot_log_type", "warn,ban,softban,timeout,configuration")
            .OldAnnotation("Npgsql:Enum:operation_type", "create,update,delete")
            .OldAnnotation(
                "Npgsql:Enum:snowflake_type",
                "none,user,role,channel,bot_log_channel,audit_log_channel"
            );

        migrationBuilder.AlterColumn<DateTime>(
            name: "UpdatedAt",
            table: "Guilds",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Guilds",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz",
            oldDefaultValueSql: "now()"
        );

        migrationBuilder.AddPrimaryKey(name: "PK_Users", table: "Users", column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Guilds_GuildId",
            table: "Users",
            column: "GuildId",
            principalTable: "Guilds",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }
}
