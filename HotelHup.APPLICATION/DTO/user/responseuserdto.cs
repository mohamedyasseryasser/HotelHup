using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{
    public class responseuserdto
    {
        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

       public string role { get; set; } 
        public bool IsActive { get; set; } = true;
    }
    public class responseuserdatabasedto
    {
        public User user { get; set; }=null!;
        public string password {  get; set; }=string.Empty;
    }
}
