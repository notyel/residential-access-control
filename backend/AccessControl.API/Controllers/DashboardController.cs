using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
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
        private readonly IUnitScope _unitScope;

        public DashboardController(IDashboardService dashboardService, ICurrentUser currentUser, IUnitScope unitScope)
        {
            _unitScope = unitScope;
            _dashboardService = dashboardService;
            _currentUser = currentUser;
        }

        // Con visits.read se ven las cifras del conjunto; sin él, solo las de las unidades con vínculo vigente
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            IReadOnlyCollection<Guid>? scope = _currentUser.HasPermission(Permissions.VisitsRead)
                ? null
                : await _unitScope.GetAccessibleUnitIdsAsync();
            var dashboard = await _dashboardService.GetDashboardAsync(scope);
            return Ok(new ResponseModel<DashboardDto>(true, "Dashboard data retrieved successfully.", dashboard));
        }
    }
}
