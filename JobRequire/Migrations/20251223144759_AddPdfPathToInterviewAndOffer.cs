using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRequire.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfPathToInterviewAndOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "OfferDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "InterviewDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "OfferDetails");

            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "InterviewDetails");
        }
    }
}
