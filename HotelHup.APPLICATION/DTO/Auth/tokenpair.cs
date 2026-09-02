using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.Auth
{
    public sealed class TokenPair
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; init; }
        public RefreshToken RefreshTokenEntity { get; init; } = null!;
    }
}
