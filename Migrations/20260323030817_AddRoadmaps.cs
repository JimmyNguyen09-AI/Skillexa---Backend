using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roadmaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roadmap_courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    roadmap_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmap_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_roadmap_courses_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roadmap_courses_roadmaps_roadmap_id",
                        column: x => x.roadmap_id,
                        principalTable: "roadmaps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_courses_course_id",
                table: "roadmap_courses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_courses_roadmap_id_course_id",
                table: "roadmap_courses",
                columns: new[] { "roadmap_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_courses_roadmap_id_order_index",
                table: "roadmap_courses",
                columns: new[] { "roadmap_id", "order_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmaps_slug",
                table: "roadmaps",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roadmap_courses");

            migrationBuilder.DropTable(
                name: "roadmaps");
        }
    }
}
