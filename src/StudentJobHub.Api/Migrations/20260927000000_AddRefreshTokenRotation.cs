using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentJobHub.Api.Data;

#nullable disable

namespace StudentJobHub.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260927000000_AddRefreshTokenRotation")]
public partial class AddRefreshTokenRotation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RefreshTokenExpiresAt",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RefreshTokenHash",
            table: "AspNetUsers",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RefreshTokenExpiresAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "RefreshTokenHash",
            table: "AspNetUsers");
    }
}