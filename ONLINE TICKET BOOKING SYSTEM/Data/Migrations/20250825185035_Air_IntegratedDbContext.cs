using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Migrations
{
    /// <inheritdoc />
    public partial class Air_IntegratedDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airlines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IataCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AllianceCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IataCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Itineraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cabin = table.Column<int>(type: "int", nullable: false),
                    TotalBase = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itineraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AirlineId = table.Column<int>(type: "int", nullable: false),
                    FromAirportId = table.Column<int>(type: "int", nullable: false),
                    ToAirportId = table.Column<int>(type: "int", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DepTimeLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    ArrTimeLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    OperatingDaysMask = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightSchedules_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightSchedules_Airports_FromAirportId",
                        column: x => x.FromAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightSchedules_Airports_ToAirportId",
                        column: x => x.ToAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AirBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pnr = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ItineraryId = table.Column<int>(type: "int", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    Infants = table.Column<int>(type: "int", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AirBookings_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FareClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightScheduleId = table.Column<int>(type: "int", nullable: false),
                    Rbd = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Cabin = table.Column<int>(type: "int", nullable: false),
                    SeatsAvailable = table.Column<int>(type: "int", nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxesAndFees = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Baggage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Refundable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FareClasses_FlightSchedules_FlightScheduleId",
                        column: x => x.FlightScheduleId,
                        principalTable: "FlightSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItineraryId = table.Column<int>(type: "int", nullable: false),
                    FlightScheduleId = table.Column<int>(type: "int", nullable: false),
                    TravelDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightSegments_FlightSchedules_FlightScheduleId",
                        column: x => x.FlightScheduleId,
                        principalTable: "FlightSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightSegments_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Passengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dob = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PassportNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AirBookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passengers_AirBookings_AirBookingId",
                        column: x => x.AirBookingId,
                        principalTable: "AirBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AirBookings_ItineraryId",
                table: "AirBookings",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_AirBookings_Pnr",
                table: "AirBookings",
                column: "Pnr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_IataCode",
                table: "Airlines",
                column: "IataCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IataCode",
                table: "Airports",
                column: "IataCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_FlightScheduleId_Rbd",
                table: "FareClasses",
                columns: new[] { "FlightScheduleId", "Rbd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightSchedules_AirlineId",
                table: "FlightSchedules",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSchedules_FromAirportId",
                table: "FlightSchedules",
                column: "FromAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSchedules_ToAirportId",
                table: "FlightSchedules",
                column: "ToAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSegments_FlightScheduleId",
                table: "FlightSegments",
                column: "FlightScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightSegments_ItineraryId",
                table: "FlightSegments",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_AirBookingId",
                table: "Passengers",
                column: "AirBookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FareClasses");

            migrationBuilder.DropTable(
                name: "FlightSegments");

            migrationBuilder.DropTable(
                name: "Passengers");

            migrationBuilder.DropTable(
                name: "FlightSchedules");

            migrationBuilder.DropTable(
                name: "AirBookings");

            migrationBuilder.DropTable(
                name: "Airlines");

            migrationBuilder.DropTable(
                name: "Airports");

            migrationBuilder.DropTable(
                name: "Itineraries");
        }
    }
}
