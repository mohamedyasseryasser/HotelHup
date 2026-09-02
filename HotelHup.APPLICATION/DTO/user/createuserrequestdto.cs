using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public sealed class CreateUserRequestDto
    {
        [Required, StringLength(256, MinimumLength = 3)]
        public string UserName { get; init; } = string.Empty;

        [Required, StringLength(200, MinimumLength = 2)]
        public string FullName { get; init; } = string.Empty;

        [StringLength(1000)]
        public string? Address { get; init; }

        public int? PropertyId { get; init; }

        [Required, StringLength(128, MinimumLength = 6)]
        public string Password { get; init; } = string.Empty;

        public IReadOnlyList<string>? RoleIds { get; init; }
    }
}
