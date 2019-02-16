using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddLoadedToPluginTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Loaded",
                table: "Plugins",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Loaded",
                table: "Plugins");
        }
    }
}
