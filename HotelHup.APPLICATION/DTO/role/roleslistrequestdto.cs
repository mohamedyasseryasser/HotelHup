using HotelHup.APPLICATION.DTO.General;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.role
{
    public sealed class RoleListRequestDto
    {
        public pagination Pg { get; init; } = new();

        [MaxLength(256)]
        public string? Search { get; init; }

        public bool? IsActive { get; init; }

        [MaxLength(256)]
        public string? PermissionName { get; init; }
    }
}
