using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePluginHostsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginHosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PluginHosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HostUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NetworkName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginHosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PluginHosts_NetworkName",
                table: "PluginHosts",
                column: "NetworkName",
                unique: true);
        }
    }
}
