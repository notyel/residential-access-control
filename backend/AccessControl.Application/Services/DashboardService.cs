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

        public DashboardService(IRepository<Visit> visitRepository)
        {
            _visitRepository = visitRepository;
        }

        public async Task<IEnumerable<LatestVisitDto>> GetLatestVisitsAsync(int count)
        {
            return await _visitRepository.GetQueryable()
                .Include(v => v.Residence)
                .ThenInclude(r => r.Owner)
                .OrderByDescending(v => v.CreatedAt)
                .Take(count)
                .Select(v => new LatestVisitDto
                {
                    VisitorFullName = v.VisitorName,
                    ResidentFullName = $"{v.Residence.Owner.FirstName} {v.Residence.Owner.LastName}",
                    EntryTime = v.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> GetTotalVisitsThisMonthAsync()
        {
            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return await _visitRepository.GetQueryable()
                .CountAsync(v => v.CreatedAt >= firstDayOfMonth && v.CreatedAt <= lastDayOfMonth);
        }
    }
}
