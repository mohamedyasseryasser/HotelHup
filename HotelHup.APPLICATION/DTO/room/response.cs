using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.room
{

    public sealed class RoomListRequest
    {
        public int? PropertyId { get; init; }
        public bool IncludeInactive { get; init; }
        [Range(1, 100)] public int PageSize { get; init; } = 20;
        [Range(1, int.MaxValue)] public int PageNumber { get; init; } = 1;
        public string? Search { get; init; }
    }

    public class RoomResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public int RoomTypeId { get; init; }
        public string RoomNumber { get; init; } = string.Empty;
        public RoomStatus Status { get; init; }
        public bool IsActive { get; init; }
        public decimal PricePerNight { get; set; }

        public string RowVersion { get; init; } = string.Empty;
    }

    public sealed class RoomListItemResponse : RoomResponse { }
}
