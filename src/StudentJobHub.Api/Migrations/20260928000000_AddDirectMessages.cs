using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentJobHub.Api.Data;

#nullable disable

namespace StudentJobHub.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260928000000_AddDirectMessages")]
public partial class AddDirectMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DirectMessages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ApplicationId = table.Column<int>(type: "int", nullable: false),
                SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DirectMessages", message => message.Id);
                table.ForeignKey(
                    name: "FK_DirectMessages_AspNetUsers_SenderId",
                    column: message => message.SenderId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DirectMessages_JobApplications_ApplicationId",
                    column: message => message.ApplicationId,
                    principalTable: "JobApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DirectMessages_ApplicationId",
            table: "DirectMessages",
            column: "ApplicationId");

        migrationBuilder.CreateIndex(
            name: "IX_DirectMessages_SenderId",
            table: "DirectMessages",
            column: "SenderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DirectMessages");
    }
}