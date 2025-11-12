using System;
using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Common
{
    public class PaginatedResultDto<T>
    {
        public IEnumerable<T>? Items { get; set; }
        public int TotalCount { get; set; }

        // Pagination metadata
        public int PageNumber { get; set; } // 1-based
        public int PageSize { get; set; }
        public int PageIndex => Math.Max(PageNumber - 1, 0); // 0-based
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
