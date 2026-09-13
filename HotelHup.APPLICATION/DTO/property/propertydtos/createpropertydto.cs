using HotelHup.APPLICATION.DTO.property.propertysettingdto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydto
{
    public sealed class CreatePropertyRequest : PropertyRequestDto
    {
        [Required]
        public PropertySettingRequestDto Settings { get; init; } = new();
    }
}
