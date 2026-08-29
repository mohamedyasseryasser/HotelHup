using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{
    public class Service:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? RevenueCategory { get; set; }

        public decimal Price { get; set; }

        public decimal Tax { get; set; }

        public bool IsActive { get; set; }


        // Navigation
        [Required]
        public int propertyid {  get; set; }
        [ForeignKey(nameof(propertyid))]
        public Property? Property { get; set; }
        public ICollection<FolioItem> FolioItems { get; set; }
            = new List<FolioItem>();
    }
}