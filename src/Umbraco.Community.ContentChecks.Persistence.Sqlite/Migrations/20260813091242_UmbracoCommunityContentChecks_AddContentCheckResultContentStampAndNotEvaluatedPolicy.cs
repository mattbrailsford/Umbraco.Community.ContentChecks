using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Community.ContentChecks.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoCommunityContentChecks_AddContentCheckResultContentStampAndNotEvaluatedPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentStamp",
                table: "umbracoCommunityContentChecksResult",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotEvaluatedPolicy",
                table: "umbracoCommunityContentChecksResult",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentStamp",
                table: "umbracoCommunityContentChecksResult");

            migrationBuilder.DropColumn(
                name: "NotEvaluatedPolicy",
                table: "umbracoCommunityContentChecksResult");
        }
    }
}
