using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddRuleInterfaceType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterfaceType",
                table: "RuleInterfaceTemplates",
                nullable: false,
                defaultValue: RuleInterfaceType.Unknown);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterfaceType",
                table: "RuleInterfaceTemplates");
        }
    }
}
