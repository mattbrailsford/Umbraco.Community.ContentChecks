using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Community.ContentChecks.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoCommunityContentChecks_AddContentCheckResultContentStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentStamp",
                table: "umbracoCommunityContentChecksResult",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentStamp",
                table: "umbracoCommunityContentChecksResult");
        }
    }
}
