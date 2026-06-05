using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prm.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeAndUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Employees_ManagerEmployeeId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "ManagerEmployeeId",
                table: "Projects",
                newName: "ManagerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ManagerEmployeeId",
                table: "Projects",
                newName: "IX_Projects_ManagerUserId");

            migrationBuilder.AddColumn<int>(
                name: "ManagerUserId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagerUserId",
                table: "Employees",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Users_ManagerUserId",
                table: "Employees",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_ManagerUserId",
                table: "Projects",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Users_ManagerUserId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_ManagerUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ManagerUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "ManagerUserId",
                table: "Projects",
                newName: "ManagerEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ManagerUserId",
                table: "Projects",
                newName: "IX_Projects_ManagerEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Employees_ManagerEmployeeId",
                table: "Projects",
                column: "ManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
