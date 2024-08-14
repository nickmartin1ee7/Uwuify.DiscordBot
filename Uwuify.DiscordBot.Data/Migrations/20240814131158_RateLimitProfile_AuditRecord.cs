using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uwuify.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class RateLimitProfile_AuditRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditRecordId",
                table: "RateLimitProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitProfiles_AuditRecordId",
                table: "RateLimitProfiles",
                column: "AuditRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_RateLimitProfiles_AuditRecords_AuditRecordId",
                table: "RateLimitProfiles",
                column: "AuditRecordId",
                principalTable: "AuditRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RateLimitProfiles_AuditRecords_AuditRecordId",
                table: "RateLimitProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RateLimitProfiles_AuditRecordId",
                table: "RateLimitProfiles");

            migrationBuilder.DropColumn(
                name: "AuditRecordId",
                table: "RateLimitProfiles");
        }
    }
}
