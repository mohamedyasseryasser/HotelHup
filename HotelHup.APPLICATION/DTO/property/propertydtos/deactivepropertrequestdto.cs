using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydto
{
    public sealed class DeactivatePropertyRequest
    {
        [Required, StringLength(500, MinimumLength = 2)] public string Reason { get; init; } = string.Empty;
    }


}
