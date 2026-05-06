using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruig.Infrastructure.Common.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivitySyncedAtUtc",
                table: "Athletes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActivitySyncedAtUtc",
                table: "Athletes");
        }
    }
}
