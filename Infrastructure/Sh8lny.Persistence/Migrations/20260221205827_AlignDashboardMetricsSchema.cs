using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignDashboardMetricsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_DashboardMetrics_MetricDate",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "ActivityIncreasePercent",
                table: "DashboardMetrics");

            migrationBuilder.RenameColumn(
                name: "NewApplicants",
                table: "DashboardMetrics",
                newName: "TotalUsers");

            migrationBuilder.RenameColumn(
                name: "CompletedProjects",
                table: "DashboardMetrics",
                newName: "TotalTransactions");

            migrationBuilder.RenameColumn(
                name: "AvailableOpportunities",
                table: "DashboardMetrics",
                newName: "TotalCompanies");

            migrationBuilder.AddColumn<int>(
                name: "ActiveProjects",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveUsers",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BannedUsers",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClosedProjects",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompletedApplications",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewProjectsLast30Days",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewUsersLast30Days",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalApplications",
                table: "DashboardMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTransactionVolume",
                table: "DashboardMetrics",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IDX_DashboardMetrics_MetricDate",
                table: "DashboardMetrics",
                column: "MetricDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_DashboardMetrics_MetricDate",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "ActiveProjects",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "ActiveUsers",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "BannedUsers",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "ClosedProjects",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "CompletedApplications",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "NewProjectsLast30Days",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "NewUsersLast30Days",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "TotalApplications",
                table: "DashboardMetrics");

            migrationBuilder.DropColumn(
                name: "TotalTransactionVolume",
                table: "DashboardMetrics");

            migrationBuilder.RenameColumn(
                name: "TotalUsers",
                table: "DashboardMetrics",
                newName: "NewApplicants");

            migrationBuilder.RenameColumn(
                name: "TotalTransactions",
                table: "DashboardMetrics",
                newName: "CompletedProjects");

            migrationBuilder.RenameColumn(
                name: "TotalCompanies",
                table: "DashboardMetrics",
                newName: "AvailableOpportunities");

            migrationBuilder.AddColumn<decimal>(
                name: "ActivityIncreasePercent",
                table: "DashboardMetrics",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IDX_DashboardMetrics_MetricDate",
                table: "DashboardMetrics",
                column: "MetricDate");
        }
    }
}
