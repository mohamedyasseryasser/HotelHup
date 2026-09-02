using HotelHup.APPLICATION.DTO.user;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.Auth
{
    public sealed class LoginResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; init; }
        public ResponseUserDto User { get; init; } = null!;
    }
}
