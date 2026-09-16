using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.RatePlane
{

    public class UpdateRatePlanRequest
    {
        [Required, MaxLength(100)] public string Name { get; init; } = string.Empty;
        [Required] public RatePlanType Type { get; init; }
        [Range(0, double.MaxValue)] public decimal Price { get; init; }
        public string Rules { get; init; } = string.Empty;
        public bool IsRefundable { get; init; }
        [Required] public DateTime ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
    }

}
