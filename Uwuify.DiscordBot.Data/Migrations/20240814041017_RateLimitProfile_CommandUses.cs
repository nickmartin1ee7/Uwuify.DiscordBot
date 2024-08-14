using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uwuify.DiscordBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class RateLimitProfile_CommandUses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommandUses",
                table: "RateLimitProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CommandUses",
                table: "RateLimitProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
