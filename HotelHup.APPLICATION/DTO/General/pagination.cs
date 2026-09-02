using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.General
{
    public class pagination
    {
        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;
    }
    public  class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

}
