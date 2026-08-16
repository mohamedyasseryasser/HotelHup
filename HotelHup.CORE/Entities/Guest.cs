using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class Guest 
    {
        [Key ,Required]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }
        public string nationality { get; set; } = string.Empty; 
        [MaxLength(50)]
        public string? NationalId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
