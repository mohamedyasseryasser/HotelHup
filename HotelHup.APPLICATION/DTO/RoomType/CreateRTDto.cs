using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.RoomType
{

    public class CreateRoomTypeRequest
    {
         

        [Required, MaxLength(100)]
        public string Name { get; init; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int MaxAdults { get; init; }

        [Range(0, int.MaxValue)]
        public int MaxChildren { get; init; }

        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; init; }

        [MaxLength(500)]
        public string? Description { get; init; }
    }

}
