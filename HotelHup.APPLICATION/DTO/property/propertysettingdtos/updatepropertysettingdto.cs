using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertysettingdto
{
    public sealed class UpdatePropertySettingsRequest
    {
        public TimeSpan? CheckInTime { get; init; }
        public TimeSpan? CheckOutTime { get; init; }
        public bool? AllowEarlyCheckIn { get; init; }
   
        public TimeSpan? HotelDayClosingTime { get; init; }
        [StringLength(10)] public string? DefaultCurrency { get; init; }
        public string? TimeZone { get; init; }
        public bool? RequireDepositForReservation { get; init; }
        public bool? RequireFullPaymentBeforeCheckOut { get; init; }
        public bool? AllowOverpayment { get; init; }
        public bool? RequireInspectionBeforeAvailable { get; init; }
        public string? Reason { get; init; }
    }
}
