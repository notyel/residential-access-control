using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Visit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OwnerOnly")]
    public class OwnersController : ControllerBase
    {
        private readonly IOwnersService _ownersService;
        public OwnersController(IOwnersService ownersService) => _ownersService = ownersService;

        [HttpGet("visits")]
        public async Task<IActionResult> MyVisits()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new ResponseModel<string>(false, "Invalid user.", null));

                var userId = Guid.Parse(userIdStr);
                var visits = await _ownersService.GetMyVisitsAsync(userId);
                return Ok(new ResponseModel<IEnumerable<VisitDto>>(true, "Visits retrieved successfully.", visits));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving visits.", null, new() { ex.Message }));
            }
        }
    }
}
