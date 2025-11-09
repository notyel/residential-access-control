using AccessControl.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("latest-visits")]
        public async Task<IActionResult> GetLatestVisits()
        {
            var visits = await _dashboardService.GetLatestVisitsAsync(10);
            return Ok(visits);
        }

        [HttpGet("total-visits-this-month")]
        public async Task<IActionResult> GetTotalVisitsThisMonth()
        {
            var count = await _dashboardService.GetTotalVisitsThisMonthAsync();
            return Ok(new { count });
        }
    }
}
