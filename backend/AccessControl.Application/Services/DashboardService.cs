using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private const int LatestVisitsCount = 10;

        private readonly IRepository<Visit> _visitRepository;
        private readonly IDateTimeService _dateTimeService;

        public DashboardService(IRepository<Visit> visitRepository, IDateTimeService dateTimeService)
        {
            _visitRepository = visitRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<DashboardDto> GetDashboardAsync(Guid? ownerId = null)
        {
            var visits = _visitRepository.GetQueryable();
            if (ownerId.HasValue)
            {
                visits = visits.Where(v => v.Residence.OwnerId == ownerId.Value);
            }

            // month boundaries follow the configured business time zone, compared in UTC
            var (startUtc, endUtc) = _dateTimeService.GetCurrentMonthRangeUtc();
            var (todayStartUtc, todayEndUtc) = _dateTimeService.GetTodayRangeUtc();

            return new DashboardDto
            {
                TotalVisitsThisMonth = await visits.CountAsync(v => v.CreatedAt >= startUtc && v.CreatedAt < endUtc),
                VisitorsInside = await visits.CountAsync(v => v.CheckOut == null),
                VisitsToday = await visits.CountAsync(v => v.CreatedAt >= todayStartUtc && v.CreatedAt < todayEndUtc),
                LatestVisits = await visits
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(LatestVisitsCount)
                    .Select(v => new LatestVisitDto
                    {
                        VisitorFullName = v.Person.FirstName + " " + v.Person.LastName,
                        ResidentFullName = v.Residence.Owner.FirstName + " " + v.Residence.Owner.LastName,
                        EntryTime = v.CreatedAt,
                        CheckOut = v.CheckOut
                    })
                    .ToListAsync()
            };
        }
    }
}
