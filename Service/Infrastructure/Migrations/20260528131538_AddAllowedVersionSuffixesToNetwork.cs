using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedVersionSuffixesToNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllowedVersionSuffix",
                columns: table => new
                {
                    Suffix = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NetworkName = table.Column<string>(type: "character varying(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedVersionSuffix", x => new { x.NetworkName, x.Suffix });
                    table.ForeignKey(
                        name: "FK_AllowedVersionSuffix_Networks_NetworkName",
                        column: x => x.NetworkName,
                        principalTable: "Networks",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowedVersionSuffix");
        }
    }
}
