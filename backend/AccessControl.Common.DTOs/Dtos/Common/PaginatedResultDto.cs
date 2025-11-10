using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Common
{
    public class PaginatedResultDto<T>
    {
        public IEnumerable<T>? Items { get; set; }
        public int TotalCount { get; set; }
    }
}
