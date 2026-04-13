using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyAiAgentUsageWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ai_agent_usage_date_utc",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET ai_agent_usage_count = 0,
                    ai_agent_usage_date_utc = NULL
                WHERE membership_plan = 'Free';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_agent_usage_date_utc",
                table: "users");
        }
    }
}
