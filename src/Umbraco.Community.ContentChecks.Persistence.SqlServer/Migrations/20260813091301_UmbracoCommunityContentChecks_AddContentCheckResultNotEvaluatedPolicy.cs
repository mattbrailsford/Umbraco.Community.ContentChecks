using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umbraco.Community.ContentChecks.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class UmbracoCommunityContentChecks_AddContentCheckResultNotEvaluatedPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotEvaluatedPolicy",
                table: "umbracoCommunityContentChecksResult",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotEvaluatedPolicy",
                table: "umbracoCommunityContentChecksResult");
        }
    }
}
