using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.aduitlog
{
    public   class AddAuditLogDto
    {
         public string userid { get; set; }= string.Empty;
        public string TargetEntity { get; init; } = string.Empty;
        public string TargetEntityId { get; init; } = string.Empty;
        public int? PropertyId { get; init; }
        public string Action { get; init; } = string.Empty;
        public object? OldValues { get; init; }
        public object? NewValues { get; init; }
        [MaxLength(500)]
        public string? Reason { get; init; }
        public string? CorrelationId { get; init; }
    }
}
