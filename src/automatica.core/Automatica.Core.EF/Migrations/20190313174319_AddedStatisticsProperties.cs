using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddedStatisticsProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Trending",
                table: "NodeInstances",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrendingInterval",
                table: "NodeInstances",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrendingToCloud",
                table: "NodeInstances",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrendingType",
                table: "NodeInstances",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Trendings",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2NodeInstance = table.Column<Guid>(nullable: false),
                    Value = table.Column<double>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Source = table.Column<string>(maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trendings", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_Trendings_NodeInstances_This2NodeInstance",
                        column: x => x.This2NodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trendings_This2NodeInstance",
                table: "Trendings",
                column: "This2NodeInstance");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trendings");

            migrationBuilder.DropColumn(
                name: "Trending",
                table: "NodeInstances");

            migrationBuilder.DropColumn(
                name: "TrendingInterval",
                table: "NodeInstances");

            migrationBuilder.DropColumn(
                name: "TrendingToCloud",
                table: "NodeInstances");

            migrationBuilder.DropColumn(
                name: "TrendingType",
                table: "NodeInstances");
        }
    }
}
