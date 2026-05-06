using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruig.Infrastructure.Common.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddStravaWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StravaWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ObjectId = table.Column<long>(type: "bigint", nullable: false),
                    AspectType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    EventTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StravaWebhookEvents_ObjectType_ObjectId_AspectType_EventTim~",
                table: "StravaWebhookEvents",
                columns: new[] { "ObjectType", "ObjectId", "AspectType", "EventTimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StravaWebhookEvents_OwnerId",
                table: "StravaWebhookEvents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_StravaWebhookEvents_ProcessedAtUtc",
                table: "StravaWebhookEvents",
                column: "ProcessedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StravaWebhookEvents");
        }
    }
}
