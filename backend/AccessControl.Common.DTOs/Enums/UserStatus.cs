namespace AccessControl.Shared.Enums
{
    public enum UserStatus
    {
        /// <summary>Creado, aún no ha definido su contraseña.</summary>
        Invited = 0,
        Active = 1,
        /// <summary>No puede iniciar sesión; conserva sus roles y su historial.</summary>
        Disabled = 2
    }

    public enum UserTokenPurpose
    {
        Invitation = 0,
        PasswordReset = 1
    }
}
