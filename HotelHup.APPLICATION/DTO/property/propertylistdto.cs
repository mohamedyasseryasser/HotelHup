using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property
{
    public sealed class PropertyListRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? Search { get; init; }
        public PropertyStatus? Status { get; init; }
        public string SortBy { get; init; } = "Name";
        public bool Descending { get; init; }
    }

    public sealed class PropertyListResponse
    {
        public IReadOnlyList<PropertyResponse> Items { get; init; } = Array.Empty<PropertyResponse>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
