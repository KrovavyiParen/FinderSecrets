using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeyRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "found_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    history_id = table.Column<int>(type: "integer", nullable: false),
                    token_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_preview = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_found_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scan_history",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    input_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    input_preview = table.Column<string>(type: "text", nullable: false),
                    secrets_found = table.Column<int>(type: "integer", nullable: false),
                    scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scan_requests",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    input_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    input_data = table.Column<string>(type: "text", nullable: false),
                    secrets_count = table.Column<int>(type: "integer", nullable: false),
                    scan_duration = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "found_secrets",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id = table.Column<int>(type: "integer", nullable: false),
                    secret_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    secret_value = table.Column<string>(type: "text", nullable: false),
                    variable_name = table.Column<string>(type: "text", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    first_found_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_found_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_found_secrets", x => x.id);
                    table.ForeignKey(
                        name: "FK_found_secrets_scan_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "public",
                        principalTable: "scan_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_found_secrets_request_id",
                schema: "public",
                table: "found_secrets",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "found_secrets",
                schema: "public");

            migrationBuilder.DropTable(
                name: "found_tokens");

            migrationBuilder.DropTable(
                name: "scan_history",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "scan_requests",
                schema: "public");
        }
    }
}
