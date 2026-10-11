using System;

namespace AccessControl.Application.Interfaces
{
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
        TimeZoneInfo TimeZone { get; }

        /// <summary>Fecha de hoy en la zona horaria del conjunto: con ella se decide qué está vigente.</summary>
        DateOnly Today { get; }

        /// <summary>Fecha y hora de este instante en la zona horaria del conjunto.</summary>
        DateTime LocalNow { get; }

        /// <summary>
        /// Normaliza a UTC. Un valor sin Kind (sin offset) se interpreta en la zona horaria configurada.
        /// </summary>
        DateTime ToUtc(DateTime value);

        /// <summary>
        /// Rango [Start, End) en UTC del mes en curso según la zona horaria configurada.
        /// </summary>
        (DateTime StartUtc, DateTime EndUtc) GetCurrentMonthRangeUtc();

        /// <summary>
        /// Rango [Start, End) en UTC del día en curso según la zona horaria configurada.
        /// </summary>
        (DateTime StartUtc, DateTime EndUtc) GetTodayRangeUtc();
    }
}
