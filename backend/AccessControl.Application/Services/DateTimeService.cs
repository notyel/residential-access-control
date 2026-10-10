using AccessControl.Application.Interfaces;
using AccessControl.Application.Settings;
using Microsoft.Extensions.Options;
using System;

namespace AccessControl.Application.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeProvider _timeProvider;

        public DateTimeService(IOptions<DateTimeSettings> settings, TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
        }

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public TimeZoneInfo TimeZone { get; }

        public DateTime ToUtc(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return value;
                case DateTimeKind.Local:
                    return value.ToUniversalTime();
                default:
                    // hora inexistente por salto de horario de verano: se corre al siguiente instante válido
                    if (TimeZone.IsInvalidTime(value))
                    {
                        value = value.AddHours(1);
                    }
                    return TimeZoneInfo.ConvertTimeToUtc(value, TimeZone);
            }
        }

        public (DateTime StartUtc, DateTime EndUtc) GetCurrentMonthRangeUtc()
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone);
            var firstDayOfMonth = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            return (ToUtc(firstDayOfMonth), ToUtc(firstDayOfMonth.AddMonths(1)));
        }
    }
}
