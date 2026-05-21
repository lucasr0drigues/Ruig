using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruig.Infrastructure.Common.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class PrivacyMinimizeAthleteActivityData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Athletes_ExternalAthleteId",
                table: "Athletes");

            migrationBuilder.DropIndex(
                name: "IX_Activities_AthleteId_ExternalActivityId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_DeletedAtUtc",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_StartedAtUtc",
                table: "Activities");

            migrationBuilder.Sql(
                """
                DELETE FROM "Activities"
                WHERE "LocalDate" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Activities"
                WHERE "Id" IN (
                    SELECT "Id"
                    FROM (
                        SELECT
                            "Id",
                            ROW_NUMBER() OVER (
                                PARTITION BY "AthleteId", "LocalDate"
                                ORDER BY "Id"
                            ) AS "RowNumber"
                        FROM "Activities"
                    ) AS "RankedActivities"
                    WHERE "RowNumber" > 1
                );
                """);

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "ExternalAthleteId",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "ExternalCreatedAt",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "ExternalUpdatedAt",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "ProfileMedium",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Athletes");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "DistanceMeters",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ElapsedTimeSeconds",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ExternalActivityId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "MovingTimeSeconds",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TotalElevationGainMeters",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "UtcOffsetAtStart",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "map_external_map_id",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "map_summary_polyline",
                table: "Activities");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "LocalDate",
                table: "Activities",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AthleteId_LocalDate",
                table: "Activities",
                columns: new[] { "AthleteId", "LocalDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activities_AthleteId_LocalDate",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Athletes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Athletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Athletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAthleteId",
                table: "Athletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalCreatedAt",
                table: "Athletes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalUpdatedAt",
                table: "Athletes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Profile",
                table: "Athletes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileMedium",
                table: "Athletes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Sex",
                table: "Athletes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Athletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Athletes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "LocalDate",
                table: "Activities",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "Activities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceMeters",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElapsedTimeSeconds",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalActivityId",
                table: "Activities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MovingTimeSeconds",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Activities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sport",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAtUtc",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalElevationGainMeters",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "UtcOffsetAtStart",
                table: "Activities",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "map_external_map_id",
                table: "Activities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "map_summary_polyline",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_ExternalAthleteId",
                table: "Athletes",
                column: "ExternalAthleteId",
                unique: true);

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
                name: "IX_Activities_StartedAtUtc",
                table: "Activities",
                column: "StartedAtUtc");
        }
    }
}
