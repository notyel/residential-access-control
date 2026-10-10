using AccessControl.Application.Services;
using AccessControl.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AccessControl.Tests.Unit
{
    public class DateTimeServiceTests
    {
        private static DateTimeService Create(DateTimeOffset utcNow, string timeZoneId = "America/Bogota")
        {
            return new DateTimeService(
                Options.Create(new DateTimeSettings { TimeZoneId = timeZoneId }),
                new FakeTimeProvider(utcNow));
        }

        [Fact]
        public void El_mes_en_curso_se_corta_en_la_zona_horaria_del_negocio()
        {
            // 1 de marzo 03:00 UTC todavía es 28 de febrero 22:00 en Bogotá (UTC-5)
            var service = Create(new DateTimeOffset(2026, 3, 1, 3, 0, 0, TimeSpan.Zero));

            var (start, end) = service.GetCurrentMonthRangeUtc();

            Assert.Equal(new DateTime(2026, 2, 1, 5, 0, 0, DateTimeKind.Utc), start);
            Assert.Equal(new DateTime(2026, 3, 1, 5, 0, 0, DateTimeKind.Utc), end);
        }

        [Fact]
        public void Una_fecha_sin_zona_se_interpreta_en_la_zona_del_negocio()
        {
            var service = Create(DateTimeOffset.UtcNow);

            var utc = service.ToUtc(new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Unspecified));

            Assert.Equal(new DateTime(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc), utc);
        }

        [Fact]
        public void Una_fecha_utc_no_se_modifica()
        {
            var service = Create(DateTimeOffset.UtcNow);
            var value = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);

            Assert.Equal(value, service.ToUtc(value));
        }
    }
}
