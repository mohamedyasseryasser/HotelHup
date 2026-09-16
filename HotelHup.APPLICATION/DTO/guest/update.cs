using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.guest
{
    public sealed class UpdateGuestRequest : CreateGuestRequest
    {
        [Required] public string ExpectedRowVersion { get; init; } = string.Empty;
    }

    public sealed class DeactivateGuestRequest
    {
        [MaxLength(500)] public string? Reason { get; init; }
        [Required] public string ExpectedRowVersion { get; init; } = string.Empty;
    }

    public sealed class AnonymizeGuestRequest
    {
        [Required, MaxLength(500)] public string Reason { get; init; } = string.Empty;
        [Required] public string ExpectedRowVersion { get; init; } = string.Empty;
    }

}
