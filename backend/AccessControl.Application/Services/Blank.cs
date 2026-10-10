namespace AccessControl.Application.Services
{
    internal static class Blank
    {
        /// <summary>Un texto opcional vacío o en blanco se guarda como null.</summary>
        public static string? ToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
