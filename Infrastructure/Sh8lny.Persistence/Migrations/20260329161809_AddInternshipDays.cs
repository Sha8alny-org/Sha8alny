using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalInternshipDays",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalInternshipDays",
                table: "Students");
        }
    }
}
