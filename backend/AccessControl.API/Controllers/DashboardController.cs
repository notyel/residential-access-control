using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Domain.Common;
using AccessControl.Common.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.DashboardRead)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ICurrentUser _currentUser;

        public DashboardController(IDashboardService dashboardService, ICurrentUser currentUser)
        {
            _dashboardService = dashboardService;
            _currentUser = currentUser;
        }

        // Con visits.read se ven las cifras del conjunto; sin él, solo las de las residencias propias
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            Guid? ownerId = _currentUser.HasPermission(Permissions.VisitsRead) ? null : User.GetUserId();
            var dashboard = await _dashboardService.GetDashboardAsync(ownerId);
            return Ok(new ResponseModel<DashboardDto>(true, "Dashboard data retrieved successfully.", dashboard));
        }
    }
}
