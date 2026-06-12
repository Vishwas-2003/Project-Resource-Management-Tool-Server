using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prm.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectRiskEmailHistories");

            migrationBuilder.CreateTable(
                name: "EmailNotificationHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailTypeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    SentOnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailNotificationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailNotificationHistory_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailNotificationHistory_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailNotificationHistory_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailNotificationHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_CreatedByUserId",
                table: "EmailNotificationHistory",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_EmailTypeId_ProjectId_SentOnDate",
                table: "EmailNotificationHistory",
                columns: new[] { "EmailTypeId", "ProjectId", "SentOnDate" },
                unique: true,
                filter: "[EmailTypeId] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_EmailTypeId_UserId_SentOnDate",
                table: "EmailNotificationHistory",
                columns: new[] { "EmailTypeId", "UserId", "SentOnDate" },
                unique: true,
                filter: "[EmailTypeId] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_ModifiedByUserId",
                table: "EmailNotificationHistory",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_ProjectId",
                table: "EmailNotificationHistory",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationHistory_UserId",
                table: "EmailNotificationHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailNotificationHistory");

            migrationBuilder.CreateTable(
                name: "ProjectRiskEmailHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentOnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRiskEmailHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRiskEmailHistories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectRiskEmailHistories_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRiskEmailHistories_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRiskEmailHistories_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRiskEmailHistories_CreatedByUserId",
                table: "ProjectRiskEmailHistories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRiskEmailHistories_ManagerUserId",
                table: "ProjectRiskEmailHistories",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRiskEmailHistories_ModifiedByUserId",
                table: "ProjectRiskEmailHistories",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRiskEmailHistories_ProjectId_SentOnDate",
                table: "ProjectRiskEmailHistories",
                columns: new[] { "ProjectId", "SentOnDate" },
                unique: true);
        }
    }
}
