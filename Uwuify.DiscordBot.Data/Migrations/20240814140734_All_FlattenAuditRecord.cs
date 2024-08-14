using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Uwuify.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class All_FlattenAuditRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AuditRecords_AuditRecordId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_RateLimitProfiles_AuditRecords_AuditRecordId",
                table: "RateLimitProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UptimeReports_AuditRecords_AuditRecordId",
                table: "UptimeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_AuditRecords_AuditRecordId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_Users_AuditRecordId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UptimeReports_AuditRecordId",
                table: "UptimeReports");

            migrationBuilder.DropIndex(
                name: "IX_RateLimitProfiles_AuditRecordId",
                table: "RateLimitProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_AuditRecordId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "AuditRecordId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AuditRecordId",
                table: "UptimeReports");

            migrationBuilder.DropColumn(
                name: "AuditRecordId",
                table: "RateLimitProfiles");

            migrationBuilder.DropColumn(
                name: "AuditRecordId",
                table: "Guilds");

            migrationBuilder.AddColumn<DateTime>(
                name: "Creation",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Creation",
                table: "UptimeReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "UptimeReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Creation",
                table: "RateLimitProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "RateLimitProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Creation",
                table: "Guilds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Guilds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Creation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Creation",
                table: "UptimeReports");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "UptimeReports");

            migrationBuilder.DropColumn(
                name: "Creation",
                table: "RateLimitProfiles");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "RateLimitProfiles");

            migrationBuilder.DropColumn(
                name: "Creation",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Guilds");

            migrationBuilder.AddColumn<int>(
                name: "AuditRecordId",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuditRecordId",
                table: "UptimeReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuditRecordId",
                table: "RateLimitProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuditRecordId",
                table: "Guilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Creation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuditRecordId",
                table: "Users",
                column: "AuditRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_UptimeReports_AuditRecordId",
                table: "UptimeReports",
                column: "AuditRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitProfiles_AuditRecordId",
                table: "RateLimitProfiles",
                column: "AuditRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_AuditRecordId",
                table: "Guilds",
                column: "AuditRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AuditRecords_AuditRecordId",
                table: "Guilds",
                column: "AuditRecordId",
                principalTable: "AuditRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RateLimitProfiles_AuditRecords_AuditRecordId",
                table: "RateLimitProfiles",
                column: "AuditRecordId",
                principalTable: "AuditRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UptimeReports_AuditRecords_AuditRecordId",
                table: "UptimeReports",
                column: "AuditRecordId",
                principalTable: "AuditRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AuditRecords_AuditRecordId",
                table: "Users",
                column: "AuditRecordId",
                principalTable: "AuditRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
