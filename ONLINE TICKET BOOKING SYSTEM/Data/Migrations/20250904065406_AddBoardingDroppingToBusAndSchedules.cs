using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardingDroppingToBusAndSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardingPointsString",
                table: "BusSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DroppingPointsString",
                table: "BusSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoardingPointsString",
                table: "Buses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DroppingPointsString",
                table: "Buses",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BoardingPointsString", table: "BusSchedules");
            migrationBuilder.DropColumn(name: "DroppingPointsString", table: "BusSchedules");
            migrationBuilder.DropColumn(name: "BoardingPointsString", table: "Buses");
            migrationBuilder.DropColumn(name: "DroppingPointsString", table: "Buses");
        }
    }

}
