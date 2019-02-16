using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddedPluginGuidToPluginTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    PluginGuid = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    ComponentName = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true),
                    PluginType = table.Column<int>(nullable: false),
                    Publisher = table.Column<string>(nullable: true),
                    MinCoreServerVersion = table.Column<string>(nullable: true),
                    AzureUrl = table.Column<string>(nullable: true),
                    AzureFileName = table.Column<string>(nullable: true),
                    IsPublic = table.Column<bool>(nullable: true),
                    IsPrerelease = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.ObjId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Plugins");
        }
    }
}
