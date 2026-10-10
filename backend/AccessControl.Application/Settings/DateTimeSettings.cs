namespace AccessControl.Application.Settings
{
    public class DateTimeSettings
    {
        public const string SectionName = "DateTimeSettings";

        // Zona horaria del conjunto residencial (ID IANA o Windows). La BD siempre guarda UTC;
        // esta zona solo define los cortes de día/mes y cómo se interpretan fechas sin offset.
        public string TimeZoneId { get; set; } = "America/Bogota";
    }
}
