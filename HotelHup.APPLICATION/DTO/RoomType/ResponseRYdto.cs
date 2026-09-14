using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.RoomType
{

    public class RoomTypeResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int MaxAdults { get; init; }
        public int MaxChildren { get; init; }
        public decimal BasePrice { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }

}
