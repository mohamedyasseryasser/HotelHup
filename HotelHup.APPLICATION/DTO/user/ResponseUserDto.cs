using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public class ResponseUserDto
    {
        public string UserId { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Address { get; init; }
        public int? PropertyId { get; init; }
        public string? PropertyName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public IReadOnlyList<ResponseRoleDto> Roles { get; init; } = Array.Empty<ResponseRoleDto>();
    }
}
