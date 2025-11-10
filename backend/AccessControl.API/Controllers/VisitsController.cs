using AccessControl.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using AccessControl.Shared.Dtos.Visit;
using AccessControl.Shared.Enums;
using System.Threading.Tasks;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Common.DTOs.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;
        private readonly IRepository<User> _userRepository;
        public VisitsController(IVisitService visitService, IRepository<User> userRepository)
        {
            _visitService = visitService;
            _userRepository = userRepository;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> RegisterVisit([FromBody] CreateVisitDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new ResponseModel<string>(false, "Invalid user.", null));

                var userId = Guid.Parse(userIdStr);
                var visit = await _visitService.RegisterVisitAsync(dto, userId);
                return Ok(new ResponseModel<VisitDto>(true, "Visit registered successfully.", visit));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while registering the visit.", null, new() { ex.Message }));
            }
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> GetVisits([FromQuery] VisitFilterDto filter)
        {
            try
            {
                var result = await _visitService.GetVisitsAsync(filter);
                return Ok(new ResponseModel<PaginatedResultDto<VisitDto>>(true, "Visits retrieved successfully.", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving visits.", null, new() { ex.Message }));
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetVisit(Guid id)
        {
            try
            {
                var visit = await _visitService.GetVisitByIdAsync(id);
                if (visit == null) return NotFound(new ResponseModel<string>(false, "Visit not found.", null));

                if (User.IsInRole(Role.Owner.ToString()))
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new ResponseModel<string>(false, "Invalid user.", null));
                    var userId = Guid.Parse(userIdStr);

                    var user = await _userRepository.GetQueryable().Include(u => u.Residences).FirstOrDefaultAsync(u => u.Id == userId);
                    if (user == null || user.Residences == null || !user.Residences.Any(r => r.Id == visit.ResidenceId))
                    {
                        return Forbid();
                    }
                }

                return Ok(new ResponseModel<VisitDto>(true, "Visit retrieved successfully.", visit));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving the visit.", null, new() { ex.Message }));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrGuard")]
        public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitDto dto)
        {
            try
            {
                var visit = await _visitService.UpdateVisitAsync(id, dto);
                if (visit == null) return NotFound(new ResponseModel<string>(false, "Visit not found.", null));
                return Ok(new ResponseModel<VisitDto>(true, "Visit updated successfully.", visit));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while updating the visit.", null, new() { ex.Message }));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteVisit(Guid id)
        {
            try
            {
                var result = await _visitService.DeleteVisitAsync(id);
                if (!result) return NotFound(new ResponseModel<string>(false, "Visit not found.", null));
                return Ok(new ResponseModel<string>(true, "Visit deleted successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while deleting the visit.", null, new() { ex.Message }));
            }
        }

        [HttpPost("{id}/checkout")]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> Checkout(Guid id)
        {
            try
            {
                var v = await _visitService.CheckoutAsync(id);
                if (v == null) return NotFound(new ResponseModel<string>(false, "Visit not found.", null));
                return Ok(new ResponseModel<VisitDto>(true, "Checkout successful.", v));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred during checkout.", null, new() { ex.Message }));
            }
        }
    }
}
