using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prm.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigurationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemConfigurations_Provider",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxWeeklyHours",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "SchedulerInterval",
                table: "SystemConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationType",
                table: "SystemConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "SystemConfigurations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "ConfigurationType", "CreatedAtUtc", "CreatedByUserId", "ModifiedAtUtc", "ModifiedByUserId", "Value" },
                values: new object[,]
                {
                    { 1, "Provider", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "" },
                    { 2, "ApiKey", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "" },
                    { 3, "SchedulerInterval", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "" },
                    { 4, "MaxWeeklyHours", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SystemConfigurations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "ConfigurationType",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "SystemConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "SystemConfigurations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxWeeklyHours",
                table: "SystemConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "SystemConfigurations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SchedulerInterval",
                table: "SystemConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Provider",
                table: "SystemConfigurations",
                column: "Provider",
                unique: true);
        }
    }
}
