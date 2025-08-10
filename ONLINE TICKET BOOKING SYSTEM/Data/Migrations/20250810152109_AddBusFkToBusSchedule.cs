using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusFkToBusSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusId",
                table: "BusSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnBusId",
                table: "BusSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_BusId",
                table: "BusSchedules",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_ReturnBusId",
                table: "BusSchedules",
                column: "ReturnBusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusSchedules_Buses_BusId",
                table: "BusSchedules",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusSchedules_Buses_ReturnBusId",
                table: "BusSchedules",
                column: "ReturnBusId",
                principalTable: "Buses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusSchedules_Buses_BusId",
                table: "BusSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_BusSchedules_Buses_ReturnBusId",
                table: "BusSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BusSchedules_BusId",
                table: "BusSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BusSchedules_ReturnBusId",
                table: "BusSchedules");

            migrationBuilder.DropColumn(
                name: "BusId",
                table: "BusSchedules");

            migrationBuilder.DropColumn(
                name: "ReturnBusId",
                table: "BusSchedules");
        }
    }
}
