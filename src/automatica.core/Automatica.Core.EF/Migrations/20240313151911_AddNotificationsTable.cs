using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "char(36)", nullable: false),
                    This2NodeInstance = table.Column<Guid>(type: "char(36)", nullable: true),
                    This2RuleInstance = table.Column<Guid>(type: "char(36)", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "date", nullable: false),
                    DismissDate = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ObjId);
                    table.ForeignKey(
                        name: "NodeInstance2Notification",
                        column: x => x.This2NodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "RuleInstance2Notification",
                        column: x => x.This2RuleInstance,
                        principalTable: "RuleInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_This2NodeInstance",
                table: "Notifications",
                column: "This2NodeInstance");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_This2RuleInstance",
                table: "Notifications",
                column: "This2RuleInstance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
