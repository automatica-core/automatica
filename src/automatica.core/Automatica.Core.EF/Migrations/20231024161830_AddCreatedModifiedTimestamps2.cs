using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedModifiedTimestamps2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "UserGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "UserGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "Roles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Priviledges",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "Priviledges",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AreaTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "AreaTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(DateTime.Now));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Priviledges");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Priviledges");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AreaTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "AreaTemplates");
        }
    }
}
