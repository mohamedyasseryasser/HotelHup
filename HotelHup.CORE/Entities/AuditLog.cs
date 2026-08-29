using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{
    public class AuditLog:BaseEntity
    {
        [Key, Required]
        public int id { get; set; }
        public string? UserId { get; set; }

        public int? PropertyId { get; set; }

        public string EntityName { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? Reason { get; set; }

        public string? IpAddress { get; set; }

        public string? ClientContext { get; set; }

        public string? CorrelationId { get; set; }


        // Navigation

        public User? User { get; set; }

        public Property? Property { get; set; }
    }
}