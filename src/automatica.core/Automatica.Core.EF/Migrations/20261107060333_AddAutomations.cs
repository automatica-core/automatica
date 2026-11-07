using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "This2RuleInstance",
                table: "Notifications",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "This2Automation",
                table: "Notifications",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Automations",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    IsEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                    Mode = table.Column<int>(nullable: false, defaultValue: 0),
                    MaxRuns = table.Column<int>(nullable: false, defaultValue: 10),
                    This2AreaInstance = table.Column<Guid>(nullable: true),
                    This2CategoryInstance = table.Column<Guid>(nullable: true),
                    Definition = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ModifiedAt = table.Column<DateTime>(nullable: false),
                    LastRunAt = table.Column<DateTime>(nullable: true),
                    LastRunState = table.Column<int>(nullable: true),
                    LastError = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automations", x => x.ObjId);
                    table.ForeignKey(
                        name: "AreaInstance2Automation",
                        column: x => x.This2AreaInstance,
                        principalTable: "AreaInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "CategoryInstance2Automation",
                        column: x => x.This2CategoryInstance,
                        principalTable: "CategoryInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AutomationReferences",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2Automation = table.Column<Guid>(nullable: false),
                    ReferenceId = table.Column<Guid>(nullable: false),
                    ReferenceType = table.Column<int>(nullable: false),
                    Role = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationReferences", x => x.ObjId);
                    table.ForeignKey(
                        name: "Automation2AutomationReference",
                        column: x => x.This2Automation,
                        principalTable: "Automations",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRuns",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2Automation = table.Column<Guid>(nullable: false),
                    StartedAt = table.Column<DateTime>(nullable: false),
                    FinishedAt = table.Column<DateTime>(nullable: true),
                    State = table.Column<int>(nullable: false),
                    TriggerId = table.Column<string>(nullable: true),
                    Steps = table.Column<string>(nullable: true),
                    Error = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRuns", x => x.ObjId);
                    table.ForeignKey(
                        name: "Automation2AutomationRun",
                        column: x => x.This2Automation,
                        principalTable: "Automations",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_This2Automation",
                table: "Notifications",
                column: "This2Automation");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationReferences_ReferenceId",
                table: "AutomationReferences",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationReferences_This2Automation",
                table: "AutomationReferences",
                column: "This2Automation");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_This2Automation_StartedAt",
                table: "AutomationRuns",
                columns: new[] { "This2Automation", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Automations_This2AreaInstance",
                table: "Automations",
                column: "This2AreaInstance");

            migrationBuilder.CreateIndex(
                name: "IX_Automations_This2CategoryInstance",
                table: "Automations",
                column: "This2CategoryInstance");

            migrationBuilder.AddForeignKey(
                name: "Automation2Notification",
                table: "Notifications",
                column: "This2Automation",
                principalTable: "Automations",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Automation2Notification",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "AutomationReferences");

            migrationBuilder.DropTable(
                name: "AutomationRuns");

            migrationBuilder.DropTable(
                name: "Automations");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_This2Automation",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "This2Automation",
                table: "Notifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "This2RuleInstance",
                table: "Notifications",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
