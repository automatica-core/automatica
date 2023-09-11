using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToPluginModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "RuleTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "RuleInterfaceTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "PropertyTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "PropertyTemplateConstraints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "PropertyTemplateConstraintData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "NodeTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Owner",
                table: "InterfaceTypes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Owner",
                table: "RuleTemplates");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "RuleInterfaceTemplates");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "PropertyTemplates");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "PropertyTemplateConstraints");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "PropertyTemplateConstraintData");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "NodeTemplates");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "InterfaceTypes");
        }
    }
}
