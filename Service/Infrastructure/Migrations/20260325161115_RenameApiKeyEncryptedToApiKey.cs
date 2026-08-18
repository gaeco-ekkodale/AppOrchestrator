using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppOrchestrator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameApiKeyEncryptedToApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppRegistries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContainerRegistries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "PluginHosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HostUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentVariable",
                columns: table => new
                {
                    NetworkName = table.Column<string>(type: "character varying(256)", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariable", x => new { x.NetworkName, x.Id });
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_Networks_NetworkName",
                        column: x => x.NetworkName,
                        principalTable: "Networks",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StackName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DockerProjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NetworkName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StackType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    AppRegistryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PackageVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stacks_AppRegistries_AppRegistryId",
                        column: x => x.AppRegistryId,
                        principalTable: "AppRegistries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stacks_Networks_NetworkName",
                        column: x => x.NetworkName,
                        principalTable: "Networks",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRegistries_BaseUrl",
                table: "AppRegistries",
                column: "BaseUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerRegistries_ServerAddress",
                table: "ContainerRegistries",
                column: "ServerAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginHosts_NetworkName",
                table: "PluginHosts",
                column: "NetworkName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_AppRegistryId",
                table: "Stacks",
                column: "AppRegistryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_DockerProjectName",
                table: "Stacks",
                column: "DockerProjectName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_NetworkName",
                table: "Stacks",
                column: "NetworkName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerRegistries");

            migrationBuilder.DropTable(
                name: "EnvironmentVariable");

            migrationBuilder.DropTable(
                name: "PluginHosts");

            migrationBuilder.DropTable(
                name: "Stacks");

            migrationBuilder.DropTable(
                name: "AppRegistries");

            migrationBuilder.DropTable(
                name: "Networks");
        }
    }
}
