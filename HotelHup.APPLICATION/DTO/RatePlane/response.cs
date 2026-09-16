using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.RatePlane
{

    public class RatePlanResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public int RoomTypeId { get; init; }
        public string Name { get; init; } = string.Empty;
        public RatePlanType Type { get; init; }
        public bool IsActive { get; init; }
        public string RowVersion { get; init; } = string.Empty;
        public RatePlanVersionResponse? CurrentVersion { get; init; }
    }

    public class RatePlanVersionResponse
    {
        public int Id { get; init; }
        public int RatePlanId { get; init; }
        public int VersionNumber { get; init; }
        public decimal Price { get; init; }
        public string Rules { get; init; } = string.Empty;
        public bool IsRefundable { get; init; }
        public DateTime ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
        public bool IsActive { get; init; }
    }

}
