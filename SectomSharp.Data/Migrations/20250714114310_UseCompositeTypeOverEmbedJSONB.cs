using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SectomSharp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseCompositeTypeOverEmbedJSONB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommandInputEmbedBuilder",
                table: "Cases");

            migrationBuilder.AlterColumn<string>(
                name: "LogMessageUrl",
                table: "Cases",
                type: "character varying(96)",
                maxLength: 96,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Color",
                table: "Cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cases",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PerpetratorAvatarUrl",
                table: "Cases",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // language=SQL
            migrationBuilder.Sql("""
                                 DO
                                 $$
                                 BEGIN
                                     IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'embed_field') THEN
                                         CREATE TYPE embed_field AS
                                         (
                                             name  varchar(256),
                                             value varchar(1024)
                                         );
                                     END IF;
                                 END
                                 $$;
                                 """);
            
            // language=SQL
            migrationBuilder.Sql("""
                                 ALTER TABLE "Cases"
                                 ADD COLUMN IF NOT EXISTS "Fields" embed_field[] NOT NULL DEFAULT '{}'::embed_field[];
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PerpetratorAvatarUrl",
                table: "Cases");

            migrationBuilder.AlterColumn<string>(
                name: "LogMessageUrl",
                table: "Cases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(96)",
                oldMaxLength: 96,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommandInputEmbedBuilder",
                table: "Cases",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
            
            // language=SQL
            migrationBuilder.Sql("""
                                 ALTER TABLE "Cases"
                                 DROP COLUMN IF EXISTS "Fields";
                                 """);
        }
    }
}
