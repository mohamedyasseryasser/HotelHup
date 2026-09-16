using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.guest
{
    public class CreateGuestRequest
    {
        [Required, MaxLength(100)] public string FirstName { get; init; } = string.Empty;
        [Required, MaxLength(100)] public string LastName { get; init; } = string.Empty;
        [MaxLength(50)] public string? IdentityType { get; init; }
        [MaxLength(100)] public string? IdentityNumber { get; init; }
        [MaxLength(50)] public string? NationalId { get; init; }
        [Phone, MaxLength(20)] public string? Phone { get; init; }
        [EmailAddress, MaxLength(150)] public string? Email { get; init; }
        [MaxLength(200)] public string? Address { get; init; }
        [MaxLength(100)] public string? Nationality { get; init; }
        [MaxLength(1000)] public string? Preferences { get; init; }
        [MaxLength(4000)] public string? InternalNotes { get; init; }
        [MaxLength(4000)] public string? GuestVisibleNotes { get; init; }
    }
}
