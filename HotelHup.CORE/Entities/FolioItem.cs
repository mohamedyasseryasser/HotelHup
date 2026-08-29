using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class FolioItem:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public int FolioId { get; set; }

        public int? ServiceId { get; set; }

 
        public FolioItemType Type { get; set; }

        public string? Description { get; set; }

        public decimal Amount { get; set; }

        public decimal Tax { get; set; }

        public FolioItemSource Source { get; set; }

        public string? SourceReference { get; set; }

        public FolioItemStatus Status { get; set; }

        public DateTimeOffset PostedAt { get; set; }

        // Navigation Properties

        public Folio Folio { get; set; } = null!;

        public Service? Service { get; set; }

     }
}