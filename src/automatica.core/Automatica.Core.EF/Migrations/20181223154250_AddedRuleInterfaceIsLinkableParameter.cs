using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddedRuleInterfaceIsLinkableParameter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLinkableParameter",
                table: "RuleInterfaceTemplates",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLinkableParameter",
                table: "RuleInterfaceTemplates");
        }
    }
}
