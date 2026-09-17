using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.room
{
    public sealed class CreateRoomRequest
    {
        [Required, MaxLength(20)]
        public string RoomNumber { get; init; } = string.Empty;
        [Range(1, int.MaxValue)]
        public int RoomTypeId { get; init; }
        [Required]
        public decimal PricePerNight { get; set; }
    }
}
