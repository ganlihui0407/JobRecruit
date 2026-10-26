using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRequire.Migrations
{
    /// <inheritdoc />
    public partial class RemoveResumeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeLastUpdated",
                table: "JobSeekerProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeStatus",
                table: "JobSeekerProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeLastUpdated",
                table: "JobSeekerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeStatus",
                table: "JobSeekerProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft");
        }
    }
}

