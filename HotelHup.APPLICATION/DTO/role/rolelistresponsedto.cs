using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.role
{
    public sealed class RoleListResponseDto
    {
        public string RoleId { get; init; } = string.Empty;

        public string RoleName { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public IReadOnlyList<string> Permissions { get; init; }
            = Array.Empty<string>();
    }
}
