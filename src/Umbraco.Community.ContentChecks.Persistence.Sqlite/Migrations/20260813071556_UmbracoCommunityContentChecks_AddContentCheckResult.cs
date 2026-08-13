using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Community.ContentChecks.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoCommunityContentChecks_AddContentCheckResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "umbracoCommunityContentChecksResult",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    Culture = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Segment = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PathJson = table.Column<string>(type: "TEXT", nullable: false),
                    CheckAlias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: false),
                    RanAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_umbracoCommunityContentChecksResult", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_umbracoCommunityContentChecksResult_ContentKey_Culture_Segment",
                table: "umbracoCommunityContentChecksResult",
                columns: new[] { "ContentKey", "Culture", "Segment" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "umbracoCommunityContentChecksResult");
        }
    }
}
