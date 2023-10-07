using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.HyperSeries.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecordValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RecordValues",
                newName: "TrendId");

            migrationBuilder.AddColumn<Guid>(
                name: "NodeInstanceId",
                table: "RecordValues",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NodeInstanceId",
                table: "RecordValues");

            migrationBuilder.RenameColumn(
                name: "TrendId",
                table: "RecordValues",
                newName: "Id");
        }
    }
}
