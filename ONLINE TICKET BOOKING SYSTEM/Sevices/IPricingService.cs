using ONLINE_TICKET_BOOKING_SYSTEM.Models.Air;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public record PriceBreakdown(decimal baseFare, decimal tax, decimal grand);

    public interface IPricingService
    {
        PriceBreakdown Price(Itinerary itin, int adults, int children, int infants);
    }

    public class PricingService : IPricingService
    {
        public PriceBreakdown Price(Itinerary itin, int adults, int children, int infants)
        {
            // simple: itinerary snapshot totals already set; fallback: sum segment-level per-pax * pax
            if (itin.GrandTotal > 0)
                return new(itin.TotalBase, itin.TotalTax, itin.GrandTotal);

            int pax = adults + children + infants;
            decimal baseFare = itin.Segments.Sum(s => s.PaxBase) * pax;
            decimal tax = itin.Segments.Sum(s => s.PaxTax) * pax;
            return new(baseFare, tax, baseFare + tax);
        }
    }
}
