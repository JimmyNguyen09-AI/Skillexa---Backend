using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipPlansAndCourseAccessTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ai_agent_usage_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "membership_plan",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "access_tier",
                table: "courses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_agent_usage_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "membership_plan",
                table: "users");

            migrationBuilder.DropColumn(
                name: "access_tier",
                table: "courses");
        }
    }
}
