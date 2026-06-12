using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prm.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTimesheetForAccessRights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Access",
                table: "Timesheets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ALLOWED");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Access",
                table: "Timesheets");
        }
    }
}
