using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class HousekeepingTask:BaseEntity
    {
        [Key,Required]
        public int id{get;set;}
        public int RoomId { get; set; }

        public string? AssigneeId { get; set; }

        public HousekeepingTaskStatus Status { get; set; }

        public DateTimeOffset? DueAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties

        public Room Room { get; set; } = null!;

        public User? Assignee { get; set; }

    }
}