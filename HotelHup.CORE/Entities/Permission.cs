using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class Permission
    {
        [Key,Required]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Resource { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();
    }
}
