using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class Guest:BaseEntity
    {
        [Key ,Required]
        public int Id { get; set; }
     
        public string? IdentityNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
        public string? IdentityType { get; set; }

        public string? Address { get; set; }

        public string? Preferences { get; set; }

        public string? InternalNotes { get; set; }

        public string? GuestVisibleNotes { get; set; }

        public bool IsActive { get; set; } = true;
        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }
        public string nationality { get; set; } = string.Empty; 
        [MaxLength(50)]
        public string? NationalId { get; set; }
        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
