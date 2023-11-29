using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class ExtendRuleInterfaceInstanceValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ValueBool",
                table: "RuleInterfaceInstances",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueBool",
                table: "RuleInterfaceInstances");
        }
    }
}
