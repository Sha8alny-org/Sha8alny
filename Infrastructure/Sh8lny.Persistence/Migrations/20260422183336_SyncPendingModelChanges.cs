using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ProposalDocument",
                table: "Applications");

            migrationBuilder.AddColumn<string>(
                name: "CvFileUrl",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposalFileUrl",
                table: "Applications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentCvUrl",
                table: "Applications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvFileUrl",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProposalFileUrl",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "StudentCvUrl",
                table: "Applications");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposalDocument",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
