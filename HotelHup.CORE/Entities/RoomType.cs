using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class RoomType
    {
        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }

        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public bool IsActive { get; set; } = true;

        public string CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public User CreatedByUser { get; set; } = null!;

        // Navigation
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
