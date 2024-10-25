using Microsoft.EntityFrameworkCore.Migrations;
using SectomSharp.Data.Enums;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class LogChannelDesignChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AuditLogChannel_OperationType", table: "Snowflake");

            migrationBuilder.DropColumn(name: "OperationType", table: "Snowflake");

            migrationBuilder
                .AlterDatabase()
                .Annotation("Npgsql:Enum:operation_type", "create,update,delete")
                .Annotation(
                    "Npgsql:Enum:snowflake_type",
                    "none,user,role,channel,bot_log_channel,audit_log_channel"
                )
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

            migrationBuilder.AlterColumn<int>(
                name: "BotLogType",
                table: "Snowflake",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "bot_log_type",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "AuditLogType",
                table: "Snowflake",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "audit_log_type",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "LogType",
                table: "Cases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "bot_log_type"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                )
                .OldAnnotation("Npgsql:Enum:operation_type", "create,update,delete")
                .OldAnnotation(
                    "Npgsql:Enum:snowflake_type",
                    "none,user,role,channel,bot_log_channel,audit_log_channel"
                );

            migrationBuilder.AlterColumn<int>(
                name: "BotLogType",
                table: "Snowflake",
                type: "bot_log_type",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "AuditLogType",
                table: "Snowflake",
                type: "audit_log_type",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );

            migrationBuilder.AddColumn<OperationType>(
                name: "AuditLogChannel_OperationType",
                table: "Snowflake",
                type: "operation_type",
                nullable: true
            );

            migrationBuilder.AddColumn<OperationType>(
                name: "OperationType",
                table: "Snowflake",
                type: "operation_type",
                nullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "LogType",
                table: "Cases",
                type: "bot_log_type",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer"
            );
        }
    }
}
