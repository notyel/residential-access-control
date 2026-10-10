using System.Text;

namespace AccessControl.Application.Settings
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";
        public const int MinKeyBytes = 32;

        // Clave de firma HS256. Nunca va en appsettings: user-secrets en desarrollo, variable de entorno en despliegue.
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; } = 240;

        public bool HasValidKey() => Encoding.UTF8.GetByteCount(Key ?? string.Empty) >= MinKeyBytes;
    }
}
