using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class AirModuleInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightSegments_FlightSchedules_FlightScheduleId",
                table: "FlightSegments");

            migrationBuilder.DropIndex(
                name: "IX_FareClasses_FlightScheduleId_Rbd",
                table: "FareClasses");

            migrationBuilder.DropColumn(
                name: "AllianceCode",
                table: "Airlines");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Passengers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Passengers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTax",
                table: "Itineraries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBase",
                table: "Itineraries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrandTotal",
                table: "Itineraries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "Cabin",
                table: "FlightSegments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChosenRbd",
                table: "FlightSegments",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "FlightSegments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PaxBase",
                table: "FlightSegments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaxTax",
                table: "FlightSegments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Equipment",
                table: "FlightSchedules",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxesAndFees",
                table: "FareClasses",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FareClasses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseFare",
                table: "FareClasses",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Baggage",
                table: "FareClasses",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Airports",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Airports",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Airports",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Airlines",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountPaid",
                table: "AirBookings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountDue",
                table: "AirBookings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_FlightScheduleId",
                table: "FareClasses",
                column: "FlightScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlightSegments_FlightSchedules_FlightScheduleId",
                table: "FlightSegments",
                column: "FlightScheduleId",
                principalTable: "FlightSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightSegments_FlightSchedules_FlightScheduleId",
                table: "FlightSegments");

            migrationBuilder.DropIndex(
                name: "IX_FareClasses_FlightScheduleId",
                table: "FareClasses");

            migrationBuilder.DropColumn(
                name: "Cabin",
                table: "FlightSegments");

            migrationBuilder.DropColumn(
                name: "ChosenRbd",
                table: "FlightSegments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "FlightSegments");

            migrationBuilder.DropColumn(
                name: "PaxBase",
                table: "FlightSegments");

            migrationBuilder.DropColumn(
                name: "PaxTax",
                table: "FlightSegments");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTax",
                table: "Itineraries",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBase",
                table: "Itineraries",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GrandTotal",
                table: "Itineraries",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Equipment",
                table: "FlightSchedules",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxesAndFees",
                table: "FareClasses",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FareClasses",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseFare",
                table: "FareClasses",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Baggage",
                table: "FareClasses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Airports",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Airports",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Airports",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Airlines",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<string>(
                name: "AllianceCode",
                table: "Airlines",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountPaid",
                table: "AirBookings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountDue",
                table: "AirBookings",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_FlightScheduleId_Rbd",
                table: "FareClasses",
                columns: new[] { "FlightScheduleId", "Rbd" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FlightSegments_FlightSchedules_FlightScheduleId",
                table: "FlightSegments",
                column: "FlightScheduleId",
                principalTable: "FlightSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
