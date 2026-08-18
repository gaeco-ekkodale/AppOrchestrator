using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EnvironmentVariable",
                table: "EnvironmentVariable");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "EnvironmentVariable");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EnvironmentVariable",
                table: "EnvironmentVariable",
                columns: new[] { "NetworkName", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EnvironmentVariable",
                table: "EnvironmentVariable");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "EnvironmentVariable",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EnvironmentVariable",
                table: "EnvironmentVariable",
                columns: new[] { "NetworkName", "Id" });
        }
    }
}
