using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Visit> _visitRepository;
        private readonly IDateTimeService _dateTimeService;

        public DashboardService(IRepository<Visit> visitRepository, IDateTimeService dateTimeService)
        {
            _visitRepository = visitRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<IEnumerable<LatestVisitDto>> GetLatestVisitsAsync(int count)
        {
            return await _visitRepository.GetQueryable()
                .Include(v => v.Person)
                .Include(v => v.Residence)
                .ThenInclude(r => r.Owner)
                .OrderByDescending(v => v.CreatedAt)
                .Take(count)
                .Select(v => new LatestVisitDto
                {
                    VisitorFullName = $"{v.Person.FirstName} {v.Person.LastName}",
                    ResidentFullName = $"{v.Residence.Owner.FirstName} {v.Residence.Owner.LastName}",
                    EntryTime = v.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> GetTotalVisitsThisMonthAsync()
        {
            // month boundaries follow the configured business time zone, compared in UTC
            var (startUtc, endUtc) = _dateTimeService.GetCurrentMonthRangeUtc();

            return await _visitRepository.GetQueryable()
                .CountAsync(v => v.CreatedAt >= startUtc && v.CreatedAt < endUtc);
        }
    }
}
