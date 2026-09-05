using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property
{
    public sealed class CreatePropertyRequest : PropertyRequestDto
    {
        [Required]
        public PropertySettingRequestDto Settings { get; init; } = new();
    }
    public class PropertyRequestDto
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string Name { get; init; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string Code { get; init; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; init; }
      
    }

    public sealed class PropertySettingRequestDto
    {
        [Required, StringLength(10, MinimumLength = 3)]
        public string Currency { get; init; } = "USD";

        [Required, StringLength(100)]
        public string TimeZone { get; init; } = "UTC";

        public TimeSpan CheckInTime { get; init; } = new(14, 0, 0);
        public TimeSpan CheckOutTime { get; init; } = new(12, 0, 0);
        public bool AllowEarlyCheckIn { get; init; }
        public TimeSpan? HotelDayClosingTime { get; init; }
        public bool RequireDepositForReservation { get; init; }
        public bool RequireFullPaymentBeforeCheckOut { get; init; }
        public bool AllowOverpayment { get; init; }
        public bool RequireInspectionBeforeAvailable { get; init; }
    }

}
