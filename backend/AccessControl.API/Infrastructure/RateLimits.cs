namespace AccessControl.API.Infrastructure
{
    public static class RateLimits
    {
        /// <summary>Política de los endpoints de autenticación: por dirección de origen.</summary>
        public const string Auth = "auth";

        public const string SectionName = "RateLimits";
    }

    public class RateLimitSettings
    {
        /// <summary>Peticiones de autenticación permitidas por minuto desde una misma dirección.</summary>
        public int AuthPerMinute { get; set; } = 20;
    }
}
