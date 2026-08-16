using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<RoomType> RoomTypesAdded { get; set; } = new List<RoomType>();
        public ICollection<Reservation> ReservationsCreated { get; set; } = new List<Reservation>();
        public ICollection<Payment> PaymentsReceived { get; set; } = new List<Payment>();
    }
}
