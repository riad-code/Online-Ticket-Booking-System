namespace ONLINE_TICKET_BOOKING_SYSTEM.Services
{
    public interface IPnrService
    {
        string GeneratePnr();
    }

    public class PnrService : IPnrService
    {
        private static readonly char[] Pool = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
        private readonly Random _rng = new Random();

        public string GeneratePnr()
        {
            var c = new char[6];
            for (int i = 0; i < c.Length; i++)
                c[i] = Pool[_rng.Next(Pool.Length)];
            return new string(c);
        }
    }
}
