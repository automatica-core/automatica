using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitMeasurementRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Max",
                table: "HabitActions",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Min",
                table: "HabitActions",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Max",
                table: "HabitActions");

            migrationBuilder.DropColumn(
                name: "Min",
                table: "HabitActions");
        }
    }
}
