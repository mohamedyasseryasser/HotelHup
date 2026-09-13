using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydto
{
    public sealed class UpdatePropertyRequest
    {
        [StringLength(200, MinimumLength = 2)] public string? Name { get; init; }
        [StringLength(1000)] public string? Description { get; init; }
    
        [Required, StringLength(50, MinimumLength = 2)]
        public string Reason {  get; init; }
        [Required]
        public string Code { get; init; } = string.Empty;
        
    }
}
