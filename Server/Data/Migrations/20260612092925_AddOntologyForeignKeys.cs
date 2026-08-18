using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstanceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOntologyForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_OntologyClassHierarchies_OntologyVersions_OntologyVersionId",
                table: "OntologyClassHierarchies",
                column: "OntologyVersionId",
                principalTable: "OntologyVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OntologyRelations_OntologyVersions_OntologyVersionId",
                table: "OntologyRelations",
                column: "OntologyVersionId",
                principalTable: "OntologyVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OntologyClassHierarchies_OntologyVersions_OntologyVersionId",
                table: "OntologyClassHierarchies");

            migrationBuilder.DropForeignKey(
                name: "FK_OntologyRelations_OntologyVersions_OntologyVersionId",
                table: "OntologyRelations");
        }
    }
}
