using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessControl.API.Infrastructure
{
    /// <summary>
    /// Llena la sesión (CurrentUser) desde el token, después de autenticar y antes de autorizar.
    /// El conjunto sale del token; solo el superadministrador puede elegirlo, con el encabezado
    /// X-Complex-Id. Los permisos se resuelven aquí, en el servidor, en cada petición.
    /// </summary>
    public class CurrentUserMiddleware(RequestDelegate next)
    {
        public const string ComplexHeader = "X-Complex-Id";

        public async Task InvokeAsync(HttpContext context, CurrentUser currentUser, ApplicationDbContext db, IPermissionService permissions)
        {
            var principal = context.User;
            if (principal.Identity?.IsAuthenticated == true
                && Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                currentUser.UserId = userId;
                currentUser.IsSuperAdmin = principal.FindFirstValue(AppClaims.SuperAdmin) == "true";
                currentUser.ComplexId = Guid.TryParse(principal.FindFirstValue(AppClaims.ComplexId), out var complexId) ? complexId : null;
                currentUser.TimeZoneId = principal.FindFirstValue(AppClaims.TimeZone);

                if (currentUser.IsSuperAdmin)
                {
                    if (Guid.TryParse(context.Request.Headers[ComplexHeader].FirstOrDefault(), out var selectedId))
                    {
                        var complex = await db.Complexes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == selectedId);
                        if (complex != null)
                        {
                            currentUser.ComplexId = complex.Id;
                            currentUser.TimeZoneId = complex.TimeZoneId;
                        }
                    }
                }
                else
                {
                    currentUser.Permissions = await permissions.GetEffectivePermissionsAsync(userId);
                }
            }

            await next(context);
        }
    }
}
