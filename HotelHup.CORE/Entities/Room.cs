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
    public class Room:BaseEntity
    {

        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
      [Required]
        public int RoomTypeId { get; set; }
        [ForeignKey(nameof(RoomTypeId))]
        public RoomType? RoomType { get; set; }
        [Required]
        public int property_id {  get; set; }
        [ForeignKey(nameof(property_id))]
        public Property? Property { get; set; }
        public ICollection<ReservationRoom> ReservationRoom { get; set; }
            = new List<ReservationRoom>();
        public ICollection<HousekeepingTask> HousekeepingTasks { get; set; }
         = new List<HousekeepingTask>();

        public ICollection<MaintenanceTicket> MaintenanceTickets { get; set; }
            = new List<MaintenanceTicket>();
    }
}
