using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertysettingdto
{
    public sealed class PropertySettingsResponse
    {
        public int PropertyId { get; init; }
        public TimeSpan CheckInTime { get; init; }
        public TimeSpan CheckOutTime { get; init; }
        public bool AllowEarlyCheckIn { get; init; }
        public TimeSpan? HotelDayClosingTime { get; init; }
        public string DefaultCurrency { get; init; } = string.Empty;
        public string TimeZone { get; init; } = string.Empty;
        public bool RequireDepositForReservation { get; init; }
        public bool RequireFullPaymentBeforeCheckOut { get; init; }
        public bool AllowOverpayment { get; init; }
        public bool RequireInspectionBeforeAvailable { get; init; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }
}
