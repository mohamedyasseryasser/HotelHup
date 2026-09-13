using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertycancellationdtos
{
    public sealed class ChangeCancellationPolicyStatusRequest
    {
        [Required]
        public string ExpectedRowVersion { get; init; } = string.Empty;

      
        public bool Confirm { get; init; }

       
        [StringLength(500, MinimumLength = 2)]
        public string? Reason { get; init; }
    }

}
