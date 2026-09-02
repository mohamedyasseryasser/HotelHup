 using System.ComponentModel.DataAnnotations;

namespace HotelHup.APPLICATION.DTO.user
{
    public sealed class UpdateUserRequestDto
    {
        

        [StringLength(200, MinimumLength = 2),Required]
        public string? FullName { get; init; }

        [StringLength(1000),Required]
        public string? Address { get; init; }
    }
}
