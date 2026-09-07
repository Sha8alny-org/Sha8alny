using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGpaToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Gpa",
                table: "Students",
                type: "decimal(4,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IDX_Students_Gpa",
                table: "Students",
                column: "Gpa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_Students_Gpa",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Gpa",
                table: "Students");
        }
    }
}
