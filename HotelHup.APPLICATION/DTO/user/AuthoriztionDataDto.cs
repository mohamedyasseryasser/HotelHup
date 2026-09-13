using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public sealed class AuthorizationDataDto
    {
        public User Actor { get; init; } = null!;
        public IReadOnlyCollection<Role> Roles { get; init; } = [];
        public bool IsAdmin { get; init; }
    }
}
