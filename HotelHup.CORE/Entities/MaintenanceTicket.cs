using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class MaintenanceTicket:BaseEntity
    {
        [Key, Required]
        public int id { get; set; }
        public int RoomId { get; set; }

        public string? AssigneeId { get; set; }

        public string Issue { get; set; } = null!;

        public MaintenancePriority Priority { get; set; }

        public MaintenanceStatus Status { get; set; }

        public DateTimeOffset? ClosedAt { get; set; }

        // Navigation Properties

        public Room Room { get; set; } = null!;

        public User? Assignee { get; set; }
    }
}