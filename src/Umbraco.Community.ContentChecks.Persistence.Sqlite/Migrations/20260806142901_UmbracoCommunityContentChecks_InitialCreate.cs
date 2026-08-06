using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Community.ContentChecks.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoCommunityContentChecks_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoCommunityContentChecksPolicy",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentTypeKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartNodeKey = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentFolderKey = table.Column<Guid>(type: "TEXT", nullable: true),
                    RulesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoCommunityContentChecksPolicy", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "umbracoCommunityContentChecksPolicyFolder",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ParentFolderKey = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoCommunityContentChecksPolicyFolder", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoCommunityContentChecksPolicy_Alias",
                table: "umbracoCommunityContentChecksPolicy",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_umbracoCommunityContentChecksPolicy_ContentTypeKey",
                table: "umbracoCommunityContentChecksPolicy",
                column: "ContentTypeKey");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoCommunityContentChecksPolicy_ParentFolderKey",
                table: "umbracoCommunityContentChecksPolicy",
                column: "ParentFolderKey");

            migrationBuilder.CreateIndex(
                name: "IX_umbracoCommunityContentChecksPolicyFolder_ParentFolderKey",
                table: "umbracoCommunityContentChecksPolicyFolder",
                column: "ParentFolderKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoCommunityContentChecksPolicy");

            migrationBuilder.DropTable(
                name: "umbracoCommunityContentChecksPolicyFolder");
        }
    }
}
