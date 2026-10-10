using System;

namespace AccessControl.Application.Interfaces
{
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
        TimeZoneInfo TimeZone { get; }

        /// <summary>
        /// Normaliza a UTC. Un valor sin Kind (sin offset) se interpreta en la zona horaria configurada.
        /// </summary>
        DateTime ToUtc(DateTime value);

        /// <summary>
        /// Rango [Start, End) en UTC del mes en curso según la zona horaria configurada.
        /// </summary>
        (DateTime StartUtc, DateTime EndUtc) GetCurrentMonthRangeUtc();
    }
}
