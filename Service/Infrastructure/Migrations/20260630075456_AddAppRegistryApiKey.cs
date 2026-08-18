using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppRegistryApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyEncrypted",
                table: "AppRegistries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyEncrypted",
                table: "AppRegistries");
        }
    }
}
