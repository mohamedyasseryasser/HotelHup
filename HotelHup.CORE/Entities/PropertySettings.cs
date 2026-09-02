using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities;

public class PropertySettings : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public TimeSpan CheckInTime { get; set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; set; } = new(12, 0, 0);
    public bool AllowEarlyCheckIn { get; set; }
    public TimeSpan? HotelDayClosingTime { get; set; }
    [MaxLength(10)]
    public string DefaultCurrency { get; set; } = string.Empty;
    [MaxLength(100)]
    public string TimeZone { get; set; } = string.Empty;
    public bool RequireDepositForReservation { get; set; }
    public bool RequireFullPaymentBeforeCheckOut { get; set; }
    public bool AllowOverpayment { get; set; }
    public bool RequireInspectionBeforeAvailable { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Property Property { get; set; } = null!;
}