using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sh8lny.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingGpaAndDeliverablesUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedDuration",
                table: "TrainingSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternalTraining",
                table: "TrainingSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DurationType",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGpaRequired",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumGpa",
                table: "Projects",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                columns: table => new
                {
                    SubmissionFileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingSubmissionID = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => x.SubmissionFileID);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_TrainingSubmissions_TrainingSubmissionID",
                        column: x => x.TrainingSubmissionID,
                        principalTable: "TrainingSubmissions",
                        principalColumn: "TrainingSubmissionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_SubmissionFiles_FileType",
                table: "SubmissionFiles",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IDX_SubmissionFiles_Status",
                table: "SubmissionFiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IDX_SubmissionFiles_TrainingSubmissionID",
                table: "SubmissionFiles",
                column: "TrainingSubmissionID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionFiles");

            migrationBuilder.DropColumn(
                name: "ApprovedDuration",
                table: "TrainingSubmissions");

            migrationBuilder.DropColumn(
                name: "IsExternalTraining",
                table: "TrainingSubmissions");

            migrationBuilder.DropColumn(
                name: "DurationType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsGpaRequired",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "MinimumGpa",
                table: "Projects");
        }
    }
}
