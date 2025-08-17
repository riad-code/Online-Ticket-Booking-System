using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_Booking_Timestamps_And_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedAtUtc",
                table: "Bookings",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentAt",
                table: "Bookings",
                column: "PaymentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentStatus",
                table: "Bookings",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedAtUtc",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentStatus",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentAt",
                table: "Bookings");
        }
    }
}
