using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prm.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationEntityForId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailNotificationHistory_Projects_ProjectId",
                table: "EmailNotificationHistory");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "EmailNotificationHistory",
                newName: "EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailNotificationHistory_ProjectId",
                table: "EmailNotificationHistory",
                newName: "IX_EmailNotificationHistory_EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailNotificationHistory_EmailTypeId_ProjectId_SentOnDate",
                table: "EmailNotificationHistory",
                newName: "IX_EmailNotificationHistory_EmailTypeId_EntityId_SentOnDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "EmailNotificationHistory",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailNotificationHistory_EntityId",
                table: "EmailNotificationHistory",
                newName: "IX_EmailNotificationHistory_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailNotificationHistory_EmailTypeId_EntityId_SentOnDate",
                table: "EmailNotificationHistory",
                newName: "IX_EmailNotificationHistory_EmailTypeId_ProjectId_SentOnDate");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailNotificationHistory_Projects_ProjectId",
                table: "EmailNotificationHistory",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
