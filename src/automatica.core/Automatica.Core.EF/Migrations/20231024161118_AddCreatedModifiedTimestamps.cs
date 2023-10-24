using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedModifiedTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Slaves",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "Slaves",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RuleTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RuleTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RulePages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RulePages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RuleInterfaceTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RuleInterfaceTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RuleInterfaceInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RuleInterfaceInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RuleInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RuleInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PropertyTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "PropertyTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PropertyTemplateConstraints",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "PropertyTemplateConstraints",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PropertyTemplateConstraintData",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "PropertyTemplateConstraintData",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PropertyInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "PropertyInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "NodeTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "NodeTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "NodeInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "NodeInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CategoryInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "CategoryInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AreaInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "AreaInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Slaves");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Slaves");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RuleTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RuleTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RulePages");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RulePages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RuleInterfaceTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RuleInterfaceTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RuleInterfaceInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RuleInterfaceInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RuleInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RuleInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PropertyTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "PropertyTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PropertyTemplateConstraints");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "PropertyTemplateConstraints");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PropertyTemplateConstraintData");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "PropertyTemplateConstraintData");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PropertyInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "PropertyInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NodeTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "NodeTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NodeInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "NodeInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CategoryInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CategoryInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AreaInstances");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "AreaInstances");
        }
    }
}
