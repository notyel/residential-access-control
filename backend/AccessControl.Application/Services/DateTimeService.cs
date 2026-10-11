using AccessControl.Application.Interfaces;
using AccessControl.Application.Settings;
using AccessControl.Domain.Common;
using Microsoft.Extensions.Options;
using System;

namespace AccessControl.Application.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeProvider _timeProvider;

        // La zona horaria es la del conjunto de la sesión; la de appsettings solo aplica si no hay conjunto
        public DateTimeService(IOptions<DateTimeSettings> settings, TimeProvider timeProvider, ICurrentUser? currentUser = null)
        {
            _timeProvider = timeProvider;
            var timeZoneId = currentUser?.TimeZoneId;
            TimeZone = !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
                ? zone
                : TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
        }

        public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public TimeZoneInfo TimeZone { get; }

        public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone);

        public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone));

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

        public (DateTime StartUtc, DateTime EndUtc) GetTodayRangeUtc()
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone);
            var today = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified);

            return (ToUtc(today), ToUtc(today.AddDays(1)));
        }

        public (DateTime StartUtc, DateTime EndUtc) GetCurrentMonthRangeUtc()
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone);
            var firstDayOfMonth = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            return (ToUtc(firstDayOfMonth), ToUtc(firstDayOfMonth.AddMonths(1)));
        }
    }
}
