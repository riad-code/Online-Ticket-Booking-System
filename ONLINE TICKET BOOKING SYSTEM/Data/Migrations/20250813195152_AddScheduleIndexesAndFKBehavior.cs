using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleIndexesAndFKBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusSchedules_Buses_BusId",
                table: "BusSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_BusSchedules_Buses_ReturnBusId",
                table: "BusSchedules");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "BusSchedules",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OperatorName",
                table: "BusSchedules",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "BusSchedules",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // migrationBuilder.AddColumn<string>(
            //name: "BoardingPointsString",
            // table: "BusSchedules",
            // type: "nvarchar(2000)",
            //maxLength: 2000,
            // nullable: true);

            //migrationBuilder.AddColumn<string>(
            // name: "DroppingPointsString",
            // table: "BusSchedules",
            // type: "nvarchar(2000)",
            // maxLength: 2000,
            //  nullable: true);

            //migrationBuilder.AddColumn<string>(
            //name: "BoardingPointsString",
            //table: "Buses",
            //type: "nvarchar(2000)",
            //maxLength: 2000,
            // nullable: true);

            //migrationBuilder.AddColumn<string>(
            //name: "DroppingPointsString",
            //table: "Buses",
            //type: "nvarchar(2000)",
            //maxLength: 2000,
            //nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_From_To_JourneyDate",
                table: "BusSchedules",
                columns: new[] { "From", "To", "JourneyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_OperatorName",
                table: "BusSchedules",
                column: "OperatorName");

            migrationBuilder.AddForeignKey(
                name: "FK_BusSchedules_Buses_BusId",
                table: "BusSchedules",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
               onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_BusSchedules_Buses_ReturnBusId",
                table: "BusSchedules",
                column: "ReturnBusId",
                principalTable: "Buses",
                principalColumn: "Id",
              onDelete: ReferentialAction.NoAction);

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
                name: "IX_BusSchedules_From_To_JourneyDate",
                table: "BusSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BusSchedules_OperatorName",
                table: "BusSchedules");

            //migrationBuilder.DropColumn(
            //name: "BoardingPointsString",
            //table: "BusSchedules");

            // migrationBuilder.DropColumn(
            //name: "DroppingPointsString",
            // table: "BusSchedules");

            // migrationBuilder.DropColumn(
            //name: "BoardingPointsString",
            // table: "Buses");

            // migrationBuilder.DropColumn(
            //name: "DroppingPointsString",
            // table: "Buses");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "BusSchedules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OperatorName",
                table: "BusSchedules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "BusSchedules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

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
    }
}
