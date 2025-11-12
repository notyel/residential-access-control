using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var latestVisitsRaw = await _dashboardService.GetLatestVisitsAsync(10);

                // Ensure DateTime.Kind is UTC before sending to PostgreSQL (timestamp with time zone requires UTC)
                var latestVisits = latestVisitsRaw?.Select(v =>
                {
                    v.EntryTime = DateTime.SpecifyKind(v.EntryTime, DateTimeKind.Utc);
                    return v;
                }).ToList();

                var totalVisitsThisMonth = await _dashboardService.GetTotalVisitsThisMonthAsync();

                var dashboardDto = new DashboardDto
                {
                    LatestVisits = latestVisits,
                    TotalVisitsThisMonth = totalVisitsThisMonth
                };

                return Ok(new ResponseModel<DashboardDto>(true, "Dashboard data retrieved successfully.", dashboardDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<DashboardDto>(false, "An error occurred while retrieving dashboard data.", null, new() { ex.Message }));
            }
        }
    }
}
