using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Enums
{
    public enum PropertyStatus
    {
        Active,
        Inactive
    }
    public enum PropertySortBy
    {
        Name,
        Code,
        CreatedAt,
        Status
    }
    public enum UserStatus
    {
        Active,
        Inactive
    }

    public enum PolicyStatus
    {
        Active,
        Inactive
    }

    public enum TaxType
    {
        Percentage,
        FixedAmount
    }

    public enum DepositType
    {
        Percentage,
        FixedAmount
    }
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        CheckedIn,
        CheckedOut,
        Cancelled,
        NoShow
    }

    public enum BookingSource
    {
        Direct,
        Website,
        WalkIn,
        Agent,
        Other
    }

    public enum RoomStatus
    {
        Available,
        Reserved,
        Occupied,
        Dirty,
        Cleaning,
        Inspected,
        OutOfOrder,
        OutOfService
    }

    public enum FolioStatus
    {
        Open,
        Closed
    }

    public enum FolioItemType
    {
        RoomCharge,
        Service,
        Tax,
        Fee,
        Discount,
        Adjustment,
        Payment,
        Refund
    }

    public enum FolioItemSource
    {
        Room,
        Service,
        Manual,
        System
    }

    public enum FolioItemStatus
    {
        Posted,
        Voided,
        Reversed
    }

    public enum PaymentMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        BankTransfer,
        Gateway
    }

    public enum PaymentStatus
    {
        Pending,
        PartiallyPaid,
        Paid,
        Refunded,
        Failed,
        Voided
    }

    public enum RefundStatus
    {
        Pending,
        Completed,
        Failed,
        Voided
    }

    public enum HousekeepingTaskStatus
    {
        Pending,
        Cleaning,
        Completed,
        Inspected
    }

    public enum MaintenancePriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum MaintenanceStatus
    {
        Open,
        InProgress,
        Closed
    }
    public enum RatePlanType
    {
        Standard,
        Corporate,
        Seasonal,
        Weekend,
        NonRefundable
    }
}
