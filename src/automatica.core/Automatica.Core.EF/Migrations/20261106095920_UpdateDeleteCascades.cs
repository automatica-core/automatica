using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteCascades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "NodeInstance_ibfk_2",
                table: "NodeInstances");

            migrationBuilder.DropForeignKey(
                name: "PropertyInstance_ibfk_1",
                table: "PropertyInstances");

            migrationBuilder.AddForeignKey(
                name: "NodeInstance_ibfk_2",
                table: "NodeInstances",
                column: "This2NodeTemplate",
                principalTable: "NodeTemplates",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "PropertyInstance_ibfk_1",
                table: "PropertyInstances",
                column: "This2PropertyTemplate",
                principalTable: "PropertyTemplates",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "NodeInstance_ibfk_2",
                table: "NodeInstances");

            migrationBuilder.DropForeignKey(
                name: "PropertyInstance_ibfk_1",
                table: "PropertyInstances");

            migrationBuilder.AddForeignKey(
                name: "NodeInstance_ibfk_2",
                table: "NodeInstances",
                column: "This2NodeTemplate",
                principalTable: "NodeTemplates",
                principalColumn: "ObjId");

            migrationBuilder.AddForeignKey(
                name: "PropertyInstance_ibfk_1",
                table: "PropertyInstances",
                column: "This2PropertyTemplate",
                principalTable: "PropertyTemplates",
                principalColumn: "ObjId");
        }
    }
}
