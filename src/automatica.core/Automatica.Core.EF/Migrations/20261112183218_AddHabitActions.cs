using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HabitInput",
                table: "NodeInstances",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "HabitActions",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    UtcTime = table.Column<DateTime>(nullable: false),
                    LocalTime = table.Column<DateTime>(nullable: false),
                    TargetId = table.Column<Guid>(nullable: false),
                    This2AreaInstance = table.Column<Guid>(nullable: true),
                    This2CategoryInstance = table.Column<Guid>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    Origin = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    OperatingModes = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitActions", x => x.ObjId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitActions_UtcTime",
                table: "HabitActions",
                column: "UtcTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitActions");

            migrationBuilder.DropColumn(
                name: "HabitInput",
                table: "NodeInstances");
        }
    }
}
