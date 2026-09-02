using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.Auth
{
    public sealed class LoginRequestDto
    {
        [Required]
        public string UserName { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }
}
