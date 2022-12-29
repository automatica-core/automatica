using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class SettingsMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Meta",
                table: "Settings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Meta",
                table: "Settings");
        }
    }
}
