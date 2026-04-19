using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncAppDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "badges",
                table: "users",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<DateOnly>(
                name: "last_streak_date_utc",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "streak_days",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "xp",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "badges",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_streak_date_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "streak_days",
                table: "users");

            migrationBuilder.DropColumn(
                name: "xp",
                table: "users");
        }
    }
}
