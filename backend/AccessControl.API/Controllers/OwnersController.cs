using AccessControl.Persistence;
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
        private readonly ApplicationDbContext _db;
        public OwnersController(ApplicationDbContext db) => _db = db;

        [HttpGet("visits")]
        public async Task<IActionResult> MyVisits()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var visits = await _db.Visits
                                  .Include(v => v.Residence)
                                  .Where(v => v.Residence.OwnerId == userId)
                                  .OrderByDescending(v => v.CheckIn)
                                  .ToListAsync();
            return Ok(visits);
        }
    }
}
