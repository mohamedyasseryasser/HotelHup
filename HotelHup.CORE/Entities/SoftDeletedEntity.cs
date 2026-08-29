using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public abstract class SoftDeletableEntity : BaseEntity
    {
        public DateTimeOffset? DeletedAt { get; set; }

        public string? DeletedBy { get; set; }
    }
}
