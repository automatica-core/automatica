using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class AddFactoryReferenceToFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FactoryReference",
                table: "PropertyTemplates",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FactoryReference",
                table: "NodeTemplates",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FactoryReference",
                table: "InterfaceTypes",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactoryReference",
                table: "PropertyTemplates");

            migrationBuilder.DropColumn(
                name: "FactoryReference",
                table: "NodeTemplates");

            migrationBuilder.DropColumn(
                name: "FactoryReference",
                table: "InterfaceTypes");
        }
    }
}
