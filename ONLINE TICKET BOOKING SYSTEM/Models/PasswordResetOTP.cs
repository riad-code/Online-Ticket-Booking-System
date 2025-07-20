using System;
using System.ComponentModel.DataAnnotations;

namespace ONLINE_TICKET_BOOKING_SYSTEM.Models
{
    public class PasswordResetOTP
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
