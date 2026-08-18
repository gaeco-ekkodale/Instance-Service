using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstanceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstanceMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ClassificationId = table.Column<string>(type: "text", nullable: false),
                    Properties = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OntologyClassHierarchies",
                columns: table => new
                {
                    OntologyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildUri = table.Column<string>(type: "text", nullable: false),
                    ParentUri = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyClassHierarchies", x => new { x.OntologyVersionId, x.ChildUri, x.ParentUri });
                });

            migrationBuilder.CreateTable(
                name: "OntologyRelations",
                columns: table => new
                {
                    OntologyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUri = table.Column<string>(type: "text", nullable: false),
                    DomainUri = table.Column<string>(type: "text", nullable: false),
                    RangeUri = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyRelations", x => new { x.OntologyVersionId, x.PropertyUri, x.DomainUri, x.RangeUri });
                });

            migrationBuilder.CreateTable(
                name: "OntologyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OntologyId = table.Column<string>(type: "text", nullable: false),
                    Etag = table.Column<string>(type: "text", nullable: false),
                    LoadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseGuidelineReferences",
                columns: table => new
                {
                    UseCaseId = table.Column<string>(type: "text", nullable: false),
                    BucketName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Etag = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseGuidelineReferences", x => x.UseCaseId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyClassHierarchies_OntologyVersionId_ChildUri",
                table: "OntologyClassHierarchies",
                columns: new[] { "OntologyVersionId", "ChildUri" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyRelations_OntologyVersionId_DomainUri",
                table: "OntologyRelations",
                columns: new[] { "OntologyVersionId", "DomainUri" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyRelations_OntologyVersionId_RangeUri",
                table: "OntologyRelations",
                columns: new[] { "OntologyVersionId", "RangeUri" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyVersions_OntologyId_Etag",
                table: "OntologyVersions",
                columns: new[] { "OntologyId", "Etag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstanceMetadata");

            migrationBuilder.DropTable(
                name: "OntologyClassHierarchies");

            migrationBuilder.DropTable(
                name: "OntologyRelations");

            migrationBuilder.DropTable(
                name: "OntologyVersions");

            migrationBuilder.DropTable(
                name: "UseCaseGuidelineReferences");
        }
    }
}
