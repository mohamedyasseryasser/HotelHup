using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.Auth
{
    public sealed class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; init; } = string.Empty;
    }

}
