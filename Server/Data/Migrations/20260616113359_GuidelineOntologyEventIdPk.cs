using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstanceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuidelineOntologyEventIdPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OntologyVersions_OntologyId_Etag",
                table: "OntologyVersions");

            migrationBuilder.DropIndex(
                name: "IX_guideline_version_service_id",
                table: "guideline_version");

            migrationBuilder.DropColumn(
                name: "OntologyId",
                table: "OntologyVersions");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "guideline_version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OntologyId",
                table: "OntologyVersions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "service_id",
                table: "guideline_version",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OntologyVersions_OntologyId_Etag",
                table: "OntologyVersions",
                columns: new[] { "OntologyId", "Etag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guideline_version_service_id",
                table: "guideline_version",
                column: "service_id",
                unique: true,
                filter: "service_id IS NOT NULL");
        }
    }
}
