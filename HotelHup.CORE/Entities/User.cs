using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        [ForeignKey("Property")]
        public int? PropertyId { get; set; }

        public Property? Property { get; set; }

        public ICollection<AuditLog> AuditLogs { get; set; }
            = new List<AuditLog>();
    }
}
