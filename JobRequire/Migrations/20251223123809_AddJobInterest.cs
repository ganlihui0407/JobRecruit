using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRequire.Migrations
{
    /// <inheritdoc />
    public partial class AddJobInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobInterests",
                columns: table => new
                {
                    JobInterestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobVacancyId = table.Column<int>(type: "int", nullable: false),
                    JobSeekerProfileId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInterests", x => x.JobInterestId);
                    table.ForeignKey(
                        name: "FK_JobInterests_JobSeekerProfiles_JobSeekerProfileId",
                        column: x => x.JobSeekerProfileId,
                        principalTable: "JobSeekerProfiles",
                        principalColumn: "JobSeekerProfileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobInterests_JobVacancies_JobVacancyId",
                        column: x => x.JobVacancyId,
                        principalTable: "JobVacancies",
                        principalColumn: "JobVacancyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobInterests_JobSeekerProfileId_JobVacancyId",
                table: "JobInterests",
                columns: new[] { "JobSeekerProfileId", "JobVacancyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobInterests_JobVacancyId",
                table: "JobInterests",
                column: "JobVacancyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobInterests");
        }
    }
}
