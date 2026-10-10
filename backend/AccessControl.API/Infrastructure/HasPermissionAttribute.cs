using AccessControl.Domain.Common;
using Microsoft.AspNetCore.Authorization;

namespace AccessControl.API.Infrastructure
{
    /// <summary>
    /// Exige un permiso del catálogo. Con varios, basta tener uno:
    /// [HasPermission(Permissions.VisitsRead, Permissions.VisitsReadOwn)].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute(params string[] permissions) : AuthorizeAttribute, IAuthorizationRequirementData
    {
        public IReadOnlyList<string> Permissions { get; } = permissions;

        public IEnumerable<IAuthorizationRequirement> GetRequirements()
        {
            yield return new PermissionRequirement(Permissions);
        }
    }

    public class PermissionRequirement(IReadOnlyList<string> anyOf) : IAuthorizationRequirement
    {
        public IReadOnlyList<string> AnyOf { get; } = anyOf;
    }

    /// <summary>Compara contra los permisos efectivos de la sesión, resueltos en el servidor.</summary>
    public class PermissionAuthorizationHandler(ICurrentUser currentUser) : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (currentUser.UserId != null && requirement.AnyOf.Any(currentUser.HasPermission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
