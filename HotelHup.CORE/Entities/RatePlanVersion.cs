using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{
    public class RatePlanVersion:BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RatePlanId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string Rules { get; set; } = string.Empty;

        public bool IsRefundable { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Property

        [ForeignKey(nameof(RatePlanId))]
        public RatePlan RatePlan { get; set; } = null!;
    }
}