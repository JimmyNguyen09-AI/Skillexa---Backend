using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewTopicAndPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old tables that may exist from a previously-applied-but-file-removed migration.
            // Order matters: content_blocks -> practices -> topics (FK chain)
            migrationBuilder.Sql("DROP TABLE IF EXISTS interview_practice_content_blocks CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS interview_practices CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS interview_topics CASCADE;");

            migrationBuilder.CreateTable(
                name: "interview_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interview_practices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    question = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_practices", x => x.id);
                    table.ForeignKey(
                        name: "FK_interview_practices_interview_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "interview_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_practice_content_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_practice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    code_content = table.Column<string>(type: "text", nullable: true),
                    language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    callout_variant = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    image_alt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_caption = table.Column<string>(type: "text", nullable: true),
                    image_width = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_practice_content_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_interview_practice_content_blocks_interview_practices_inter~",
                        column: x => x.interview_practice_id,
                        principalTable: "interview_practices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interview_practice_content_blocks_interview_practice_id_ord~",
                table: "interview_practice_content_blocks",
                columns: new[] { "interview_practice_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "IX_interview_practices_slug",
                table: "interview_practices",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interview_practices_topic_id",
                table: "interview_practices",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_topics_slug",
                table: "interview_topics",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "interview_practice_content_blocks");
            migrationBuilder.DropTable(name: "interview_practices");
            migrationBuilder.DropTable(name: "interview_topics");
        }
    }
}
