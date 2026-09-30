using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentJobHub.Api.Data;

#nullable disable

namespace StudentJobHub.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260929000000_AddAuditLogs")]
public partial class AddAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", log => log.Id);
                table.ForeignKey(
                    name: "FK_AuditLogs_AspNetUsers_UserId",
                    column: log => log.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLogs");
    }
}
