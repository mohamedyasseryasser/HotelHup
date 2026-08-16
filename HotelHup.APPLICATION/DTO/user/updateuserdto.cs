using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public class UpdateUserDto
    {
        public string userid { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }
 
}
