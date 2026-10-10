using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Residence;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class ResidencesService : IResidencesService
    {
        private readonly IRepository<Residence> _residenceRepository;

        public ResidencesService(IRepository<Residence> residenceRepository)
        {
            _residenceRepository = residenceRepository;
        }

        public async Task<PaginatedResultDto<ResidenceDto>> SearchAsync(string? search, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var query = _residenceRepository.GetQueryable().Where(r => r.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.Identifier, pattern) ||
                    EF.Functions.ILike(r.Owner.FirstName + " " + r.Owner.LastName, pattern));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Identifier)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ResidenceDto
                {
                    Id = r.Id,
                    Identifier = r.Identifier,
                    OwnerId = r.OwnerId,
                    OwnerFullName = r.Owner.FirstName + " " + r.Owner.LastName
                })
                .ToListAsync();

            return new PaginatedResultDto<ResidenceDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
