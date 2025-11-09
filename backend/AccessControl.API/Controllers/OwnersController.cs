using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OwnerOnly")]
    public class OwnersController : ControllerBase
    {
        private readonly IRepository<Visit> _visitRepository;
        public OwnersController(IRepository<Visit> visitRepository) => _visitRepository = visitRepository;

        [HttpGet("visits")]
        public async Task<IActionResult> MyVisits()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = Guid.Parse(userIdStr);
            var visits = await _visitRepository.GetQueryable()
                .Where(v => v.Residence.OwnerId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return Ok(visits);
        }
    }
}
