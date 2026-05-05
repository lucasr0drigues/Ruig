using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruig.Infrastructure.Common.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAthleteId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Firstname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Lastname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sex = table.Column<int>(type: "integer", nullable: true),
                    ExternalCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProfileMedium = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Profile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AthleteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalActivityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Sport = table.Column<int>(type: "integer", nullable: true),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    MovingTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ElapsedTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    TotalElevationGainMeters = table.Column<double>(type: "double precision", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UtcOffsetAtStart = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    map_external_map_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    map_summary_polyline = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StravaTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AthleteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StravaAthleteId = table.Column<long>(type: "bigint", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StravaTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StravaTokens_Athletes_AthleteId",
                        column: x => x.AthleteId,
                        principalTable: "Athletes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteId",
                table: "Activities",
                column: "AthleteId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteId_ExternalActivityId",
                table: "Activities",
                columns: new[] { "AthleteId", "ExternalActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DeletedAtUtc",
                table: "Activities",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_LocalDate",
                table: "Activities",
                column: "LocalDate");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_StartedAtUtc",
                table: "Activities",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_ExternalAthleteId",
                table: "Athletes",
                column: "ExternalAthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StravaTokens_AthleteId",
                table: "StravaTokens",
                column: "AthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StravaTokens_StravaAthleteId",
                table: "StravaTokens",
                column: "StravaAthleteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "StravaTokens");

            migrationBuilder.DropTable(
                name: "Athletes");
        }
    }
}
