using AccessControl.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using AccessControl.Shared.Dtos.Visit;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;
        public VisitsController(IVisitService visitService) => _visitService = visitService;

        [HttpPost]
    [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> RegisterVisit([FromBody] CreateVisitDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = Guid.Parse(userIdStr);
            var visit = await _visitService.RegisterVisitAsync(dto, userId);
            return Ok(visit);
        }

        [HttpGet]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> GetVisits([FromQuery] VisitFilterDto filter)
        {
            var (visits, totalCount) = await _visitService.GetVisitsAsync(filter);
            return Ok(new { visits, totalCount });
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> GetVisit(Guid id)
        {
            var visit = await _visitService.GetVisitByIdAsync(id);
            if (visit == null) return NotFound();
            return Ok(visit);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitDto dto)
        {
            var visit = await _visitService.UpdateVisitAsync(id, dto);
            if (visit == null) return NotFound();
            return Ok(visit);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteVisit(Guid id)
        {
            var result = await _visitService.DeleteVisitAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/checkout")]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> Checkout(Guid id)
        {
            var v = await _visitService.CheckoutAsync(id);
            if (v == null) return NotFound();
            return Ok(v);
        }
    }
}
