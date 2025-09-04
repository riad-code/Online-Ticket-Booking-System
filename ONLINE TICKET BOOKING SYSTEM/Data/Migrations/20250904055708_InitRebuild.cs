using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitRebuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDateUtc",
                table: "ParkItems");

            migrationBuilder.DropColumn(
                name: "StartDateUtc",
                table: "ParkItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateUtc",
                table: "ParkItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateUtc",
                table: "ParkItems",
                type: "datetime2",
                nullable: true);
        }
    }
}
