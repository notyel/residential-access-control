using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    /// <summary>
    /// Los cambios de entidades se registran solos al guardar. Este servicio es para los hechos
    /// que no son un cambio de entidad (inicio de sesión, recuperación de contraseña) y para consultar.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>Agrega el hecho al contexto; queda guardado con el siguiente SaveChanges.</summary>
        void Record(string action, Guid? complexId = null, Guid? userId = null, string? userName = null,
            string? entityType = null, string? entityId = null, object? details = null);

        Task<PaginatedResultDto<AuditLogDto>> SearchAsync(AuditFilterDto filter);
    }

    public static class AuditActions
    {
        public const string Login = "auth.login";
        public const string LoginFailed = "auth.login_failed";
        public const string LoginLocked = "auth.login_locked";
        public const string LogoutAll = "auth.logout_all";
        public const string InvitationAccepted = "auth.invitation_accepted";
        public const string PasswordResetRequested = "auth.password_reset_requested";
        public const string PasswordReset = "auth.password_reset";
        public const string InvitationSent = "user.invitation_sent";
        public const string AccessRevoked = "user.access_revoked";
    }
}
