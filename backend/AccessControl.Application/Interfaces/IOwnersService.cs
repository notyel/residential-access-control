using AccessControl.Common.DTOs.Visit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IOwnersService
    {
        Task<IEnumerable<VisitDto>> GetMyVisitsAsync(Guid userId);
    }
}
