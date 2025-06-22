using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:operation_type", "create,update,delete")
                .OldAnnotation("Npgsql:Enum:operation_type", "create,update,delete")
                .OldAnnotation("Npgsql:Enum:snowflake_type", "none,user,role,channel,bot_log_channel,audit_log_channel");

            migrationBuilder.AlterColumn<string>(
                name: "Configuration",
                table: "Guilds",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Cases",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogMessageUrl",
                table: "Cases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Cases",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WebhookUrl",
                table: "AuditLogChannels",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:operation_type", "create,update,delete")
                .Annotation("Npgsql:Enum:snowflake_type", "none,user,role,channel,bot_log_channel,audit_log_channel")
                .OldAnnotation("Npgsql:Enum:operation_type", "create,update,delete");

            migrationBuilder.AlterColumn<string>(
                name: "Configuration",
                table: "Guilds",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Cases",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogMessageUrl",
                table: "Cases",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Cases",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<string>(
                name: "WebhookUrl",
                table: "AuditLogChannels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
