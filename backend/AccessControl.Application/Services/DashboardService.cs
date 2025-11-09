using AccessControl.Application.Interfaces;
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

        public async Task<IEnumerable<Visit>> GetLatestVisitsAsync(int count)
        {
            return await _visitRepository.GetQueryable()
                .OrderByDescending(v => v.CheckIn)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetTotalVisitsThisMonthAsync()
        {
            var today = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            return await _visitRepository.GetQueryable()
                .CountAsync(v => v.CheckIn >= firstDayOfMonth && v.CheckIn <= lastDayOfMonth);
        }
    }
}
