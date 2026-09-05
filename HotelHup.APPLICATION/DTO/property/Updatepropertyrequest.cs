using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property
{
    public sealed class UpdatePropertyRequest
    {
        [StringLength(200, MinimumLength = 2)] public string? Name { get; init; }
        [StringLength(1000)] public string? Description { get; init; }
        public TimeSpan? CheckInTime { get; init; }
        public TimeSpan? CheckOutTime { get; init; }
        [Required, StringLength(50, MinimumLength = 2)]
        public string Code { get; init; } = string.Empty;
        public bool? AllowEarlyCheckIn { get; init; }
        public string? ExpectedRowVersion { get; init; }
        public string? Reason { get; init; }
    }
}
