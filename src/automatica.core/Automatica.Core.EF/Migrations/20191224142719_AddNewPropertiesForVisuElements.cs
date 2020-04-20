using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddNewPropertiesForVisuElements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "MaxHeight",
                table: "VisuObjectTemplates",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "MaxWidth",
                table: "VisuObjectTemplates",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "MinHeight",
                table: "VisuObjectTemplates",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "MinWidth",
                table: "VisuObjectTemplates",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxHeight",
                table: "VisuObjectTemplates");

            migrationBuilder.DropColumn(
                name: "MaxWidth",
                table: "VisuObjectTemplates");

            migrationBuilder.DropColumn(
                name: "MinHeight",
                table: "VisuObjectTemplates");

            migrationBuilder.DropColumn(
                name: "MinWidth",
                table: "VisuObjectTemplates");
        }
    }
}
