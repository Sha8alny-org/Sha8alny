using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentForeignKeysAndDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Companies_CompanyID1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Projects_ProjectID1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Students_StudentID1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyID1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProjectID1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StudentID1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CompanyID1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProjectID1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "StudentID1",
                table: "Payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyID1",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectID1",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentID1",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyID1",
                table: "Payments",
                column: "CompanyID1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProjectID1",
                table: "Payments",
                column: "ProjectID1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StudentID1",
                table: "Payments",
                column: "StudentID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Companies_CompanyID1",
                table: "Payments",
                column: "CompanyID1",
                principalTable: "Companies",
                principalColumn: "CompanyID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Projects_ProjectID1",
                table: "Payments",
                column: "ProjectID1",
                principalTable: "Projects",
                principalColumn: "ProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Students_StudentID1",
                table: "Payments",
                column: "StudentID1",
                principalTable: "Students",
                principalColumn: "StudentID");
        }
    }
}
