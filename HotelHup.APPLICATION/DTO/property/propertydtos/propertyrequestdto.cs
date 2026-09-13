using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydto
{
    public class PropertyRequestDto
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string Name { get; init; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string Code { get; init; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; init; }

    }
}
