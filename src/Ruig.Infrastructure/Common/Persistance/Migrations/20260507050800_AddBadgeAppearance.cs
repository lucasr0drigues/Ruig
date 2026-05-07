using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruig.Infrastructure.Common.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Badges",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "purple");

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Badges",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "strava");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Badges");
        }
    }
}
