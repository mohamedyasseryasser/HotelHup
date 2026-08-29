using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class Property:BaseEntity
    {
        [Key]
        public int ID {  get; set; }
        public string Name { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public string TimeZone { get; set; } = string.Empty;

        [Required]
        public PropertyStatus Status { get; set; }

        // Navigation Properties

        public ICollection<Room> Rooms { get; set; }
            = new List<Room>();

        public ICollection<RoomType> RoomTypes { get; set; }
            = new List<RoomType>();

        public ICollection<Reservation> Reservations { get; set; }
            = new List<Reservation>();

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
        public ICollection<User> Users { get; set; }=new List<User>();
    }
}
