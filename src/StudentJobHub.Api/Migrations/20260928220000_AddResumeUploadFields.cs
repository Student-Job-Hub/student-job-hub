using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentJobHub.Api.Data;

#nullable disable

namespace StudentJobHub.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260928220000_AddResumeUploadFields")]
    public partial class AddResumeUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeFileName",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeContentType",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeFileName",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeContentType",
                table: "JobApplications");
        }
    }
}
