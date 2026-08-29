using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{

    public class Role : IdentityRole
    {
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();
    }
}
