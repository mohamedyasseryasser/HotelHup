using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class RoomType:BaseEntity
    {
        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        [ForeignKey("Property")]
        [Required]
        public int property_id {  get; set; }
        public bool IsActive { get; set; } = true;
        // Navigation
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public Property? Property { get; set; }
        public ICollection<RatePlan> RatePlans { get; set; }= new List<RatePlan>();
    }
}
