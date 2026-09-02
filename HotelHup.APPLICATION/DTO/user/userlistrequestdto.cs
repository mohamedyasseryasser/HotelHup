using HotelHup.APPLICATION.DTO.General;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.user
{

    public sealed class UserListRequestDto
    {
        [Required]
     //   public int PageNumber { get; init; } = 1;
       // [Required]
       // public int PageSize { get; init; } = 20;
        public pagination pg {  get; set; }
        public string? UserName { get; init; }
        [MaxLength(200)]
        public string? FullName { get; init; }
        public bool? IsActive { get; init; }
        public int? PropertyId { get; init; }
        public string? RoleId { get; init; }
        public DateTime? CreatedFrom { get; init; }
        public DateTime? CreatedTo { get; init; }
    }

}
