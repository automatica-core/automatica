using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddShading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShadingFacades",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Azimuth = table.Column<double>(nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                    Definition = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ModifiedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShadingFacades", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "ShadingSettings",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Definition = table.Column<string>(nullable: false),
                    ModifiedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShadingSettings", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "ShadingBlinds",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2RuleInstance = table.Column<Guid>(nullable: false),
                    This2ShadingFacade = table.Column<Guid>(nullable: true),
                    IsEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                    IsPaused = table.Column<bool>(nullable: false),
                    Definition = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ModifiedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShadingBlinds", x => x.ObjId);
                    table.ForeignKey(
                        name: "RuleInstance2ShadingBlind",
                        column: x => x.This2RuleInstance,
                        principalTable: "RuleInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "ShadingFacade2ShadingBlind",
                        column: x => x.This2ShadingFacade,
                        principalTable: "ShadingFacades",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShadingBlinds_This2RuleInstance",
                table: "ShadingBlinds",
                column: "This2RuleInstance",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShadingBlinds_This2ShadingFacade",
                table: "ShadingBlinds",
                column: "This2ShadingFacade");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShadingBlinds");

            migrationBuilder.DropTable(
                name: "ShadingSettings");

            migrationBuilder.DropTable(
                name: "ShadingFacades");
        }
    }
}
