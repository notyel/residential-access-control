using AccessControl.Application.Exceptions;
using System.Security.Claims;

namespace AccessControl.API.Infrastructure
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : throw new ForbiddenException("Sesión no válida.");
        }
    }
}
