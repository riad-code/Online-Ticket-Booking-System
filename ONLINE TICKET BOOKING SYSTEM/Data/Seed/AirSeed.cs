using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;
using System;
using System.Threading.Tasks;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Data.Seed
{
    public static class AirSeed
    {
        public static async Task EnsureAirSeedAsync(ApplicationDbContext db)
        {
            // Airports
            if (!await db.Airports.AnyAsync())
            {
                db.Airports.AddRange(
                    new Airport { IataCode = "DAC", Name = "Hazrat Shahjalal International Airport", City = "Dhaka", Country = "Bangladesh" },
                    new Airport { IataCode = "CXB", Name = "Cox's Bazar Airport", City = "Cox's Bazar", Country = "Bangladesh" },
                    new Airport { IataCode = "CGP", Name = "Shah Amanat International Airport", City = "Chattogram", Country = "Bangladesh" },
                    new Airport { IataCode = "ZYL", Name = "Osmani International Airport", City = "Sylhet", Country = "Bangladesh" }
                );
                await db.SaveChangesAsync();
            }

            // Airlines
            if (!await db.Airlines.AnyAsync())
            {
                db.Airlines.AddRange(
                    new Airline { IataCode = "BG", Name = "Biman Bangladesh Airlines" },
                    new Airline { IataCode = "VQ", Name = "Novoair" },
                    new Airline { IataCode = "BS", Name = "US-Bangla Airlines" }
                );
                await db.SaveChangesAsync();
            }

            // Flight Schedule
            if (!await db.FlightSchedules.AnyAsync())
            {
                var bg = await db.Airlines.SingleAsync(a => a.IataCode == "BG");
                var dac = await db.Airports.SingleAsync(a => a.IataCode == "DAC");
                var cxb = await db.Airports.SingleAsync(a => a.IataCode == "CXB");

                var fs1 = new FlightSchedule
                {
                    AirlineId = bg.Id,
                    FromAirportId = dac.Id,
                    ToAirportId = cxb.Id,
                    FlightNumber = "BG147",
                    DepTimeLocal = new TimeOnly(10, 30),
                    ArrTimeLocal = new TimeOnly(11, 25),
                    DurationMinutes = 55,
                    OperatingDaysMask = 127, // every day
                    Equipment = "738"
                };

                db.FlightSchedules.Add(fs1);
                await db.SaveChangesAsync();

                // Fare Classes for BG147
                db.FareClasses.AddRange(
                    new FareClass
                    {
                        FlightScheduleId = fs1.Id,
                        Rbd = "Y",
                        Cabin = CabinClass.Economy,
                        SeatsAvailable = 9,
                        BaseFare = 4500,
                        TaxesAndFees = 900,
                        Baggage = "20kg",
                        Refundable = false
                    },
                    new FareClass
                    {
                        FlightScheduleId = fs1.Id,
                        Rbd = "J",
                        Cabin = CabinClass.Business,
                        SeatsAvailable = 4,
                        BaseFare = 12000,
                        TaxesAndFees = 1500,
                        Baggage = "30kg",
                        Refundable = true
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
