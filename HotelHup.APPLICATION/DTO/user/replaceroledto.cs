using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public sealed class ReplaceUserRolesRequestDto
    {
        public IReadOnlyList<string> RoleIds { get; init; } = Array.Empty<string>();
        public string? Reason { get; init; }
    }
}
