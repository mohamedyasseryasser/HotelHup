using HotelHup.APPLICATION.DTO.General;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.guest
{
    public sealed class GuestListRequest : pagination
    {
        [MaxLength(150)] public string? Search { get; init; }
        public bool? IsActive { get; init; }
        public string SortBy { get; init; } = "name";
        public string SortDirection { get; init; } = "asc";
    }

    public sealed class GuestResponse
    {
        public int propertyid {  get; init; }
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? IdentityType { get; init; }
        public string? IdentityNumber { get; init; }
        public string? NationalId { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public string? Nationality { get; init; }
        public string? Preferences { get; init; }
        public string? InternalNotes { get; init; }
        public string? GuestVisibleNotes { get; init; }
        public bool IsActive { get; init; }
        public string RowVersion { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }

    public sealed class GuestListItemResponse
    {
        public int Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public bool IsActive { get; init; }
        public int ReservationCount { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }

    public sealed class DuplicateGuestMatchResponse
    {
        public int GuestId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string MatchType { get; init; } = string.Empty;
        public string? MaskedContact { get; init; }
    }

    public sealed class GuestReservationSummaryResponse
    {
        public int ReservationId { get; init; }
        public int PropertyId { get; init; }
        public int Status { get; init; }
        public DateTime CheckInDate { get; init; }
        public DateTime CheckOutDate { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
