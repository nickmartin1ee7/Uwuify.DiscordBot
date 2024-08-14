using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uwuify.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class All_BaseAuditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "Users",
                newName: "AuditLastUpdate");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "Users",
                newName: "AuditCreation");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "UptimeReports",
                newName: "AuditLastUpdate");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "UptimeReports",
                newName: "AuditCreation");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "RateLimitProfiles",
                newName: "AuditLastUpdate");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "RateLimitProfiles",
                newName: "AuditCreation");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "Guilds",
                newName: "AuditLastUpdate");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "Guilds",
                newName: "AuditCreation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AuditLastUpdate",
                table: "Users",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "AuditCreation",
                table: "Users",
                newName: "Creation");

            migrationBuilder.RenameColumn(
                name: "AuditLastUpdate",
                table: "UptimeReports",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "AuditCreation",
                table: "UptimeReports",
                newName: "Creation");

            migrationBuilder.RenameColumn(
                name: "AuditLastUpdate",
                table: "RateLimitProfiles",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "AuditCreation",
                table: "RateLimitProfiles",
                newName: "Creation");

            migrationBuilder.RenameColumn(
                name: "AuditLastUpdate",
                table: "Guilds",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "AuditCreation",
                table: "Guilds",
                newName: "Creation");
        }
    }
}
