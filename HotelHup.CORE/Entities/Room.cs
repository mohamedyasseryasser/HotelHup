using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class Room
    {

        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        public int RoomTypeId { get; set; }
        [ForeignKey(nameof(RoomTypeId))]
        public RoomType RoomType { get; set; } = null!;

        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerNight { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
