using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstanceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUseCaseGuidelineReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UseCaseGuidelineReferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UseCaseGuidelineReferences",
                columns: table => new
                {
                    UseCaseId = table.Column<string>(type: "text", nullable: false),
                    BucketName = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Etag = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseGuidelineReferences", x => x.UseCaseId);
                });
        }
    }
}
