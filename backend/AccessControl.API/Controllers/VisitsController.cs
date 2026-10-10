using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Domain.Common;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Shared.Dtos.Visit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;
        private readonly ICurrentUser _currentUser;

        public VisitsController(IVisitService visitService, ICurrentUser currentUser)
        {
            _visitService = visitService;
            _currentUser = currentUser;
        }

        [HttpPost]
        [HasPermission(Permissions.VisitsRegister)]
        public async Task<IActionResult> RegisterVisit([FromBody] CreateVisitDto dto)
        {
            var visit = await _visitService.RegisterVisitAsync(dto, User.GetUserId());
            return Ok(new ResponseModel<VisitDto>(true, "Visita registrada.", visit));
        }

        // Con visits.read se ven todas; con visits.read_own, solo las de las residencias propias
        [HttpGet]
        [HasPermission(Permissions.VisitsRead, Permissions.VisitsReadOwn)]
        public async Task<IActionResult> GetVisits([FromQuery] VisitFilterDto filter)
        {
            var result = await _visitService.GetVisitsAsync(filter, OwnerScope());
            return Ok(new ResponseModel<PaginatedResultDto<VisitDto>>(true, "Visitas consultadas.", result));
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.VisitsRead, Permissions.VisitsReadOwn)]
        public async Task<IActionResult> GetVisit(Guid id)
        {
            var visit = await _visitService.GetVisitByIdAsync(id, OwnerScope());
            return Ok(new ResponseModel<VisitDto>(true, "Visita consultada.", visit));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.VisitsUpdate)]
        public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitDto dto)
        {
            var visit = await _visitService.UpdateVisitAsync(id, dto);
            return Ok(new ResponseModel<VisitDto>(true, "Visita actualizada.", visit));
        }

        [HttpPost("{id}/checkout")]
        [HasPermission(Permissions.VisitsCheckout)]
        public async Task<IActionResult> Checkout(Guid id)
        {
            var visit = await _visitService.CheckoutAsync(id, User.GetUserId());
            return Ok(new ResponseModel<VisitDto>(true, "Salida registrada.", visit));
        }

        private Guid? OwnerScope() => _currentUser.HasPermission(Permissions.VisitsRead) ? null : User.GetUserId();
    }
}
