using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class RatePlan:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public int RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public RatePlanType Type { get; set; }
        public string Rules {  get; set; }= string.Empty;
        public decimal Price { get; set; }

        public bool IsRefundable { get; set; }

        public bool IsActive { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }


        // Navigation

 
        public RoomType RoomType { get; set; } = null!;
    }
}