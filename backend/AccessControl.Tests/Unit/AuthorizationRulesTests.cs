using AccessControl.Application.Access;
using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;

namespace AccessControl.Tests.Unit
{
    public class AuthorizationRulesTests
    {
        // 2026-10-14 es miércoles
        private static readonly DateOnly Wednesday = new(2026, 10, 14);

        private static VisitAuthorization Authorization(
            AuthorizationKind kind = AuthorizationKind.Period,
            DateOnly? start = null,
            DateOnly? end = null,
            string? from = null,
            string? to = null,
            params DayOfWeek[] days)
        {
            return new VisitAuthorization
            {
                Kind = kind,
                StartDate = start ?? Wednesday,
                EndDate = end ?? start ?? Wednesday,
                TimeFrom = from == null ? null : TimeOnly.Parse(from),
                TimeTo = to == null ? null : TimeOnly.Parse(to),
                DaysOfWeek = AuthorizationRules.ToMask(days.Select(d => (int)d)),
                VisitorName = "Visitante"
            };
        }

        private static DateTime At(DateOnly day, string time) => day.ToDateTime(TimeOnly.Parse(time));

        [Fact]
        public void Vale_durante_todo_el_dia_si_no_tiene_franja()
        {
            var authorization = Authorization(AuthorizationKind.Single);

            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "00:00")).IsValid);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "23:59")).IsValid);
        }

        [Fact]
        public void No_vale_antes_de_empezar_ni_despues_de_terminar()
        {
            var authorization = Authorization(start: Wednesday, end: Wednesday.AddDays(2));

            Assert.Equal(AuthorizationRules.NotYet, AuthorizationRules.IsValidAt(authorization, At(Wednesday.AddDays(-1), "23:59")).Reason);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday.AddDays(2), "23:59")).IsValid);
            Assert.Equal(AuthorizationRules.Expired, AuthorizationRules.IsValidAt(authorization, At(Wednesday.AddDays(3), "00:00")).Reason);
        }

        [Fact]
        public void Revocada_o_ya_usada_no_permite_ingresar_aunque_la_fecha_coincida()
        {
            var revoked = Authorization();
            revoked.Status = AuthorizationStatus.Revoked;
            var consumed = Authorization(AuthorizationKind.Single);
            consumed.Status = AuthorizationStatus.Consumed;

            Assert.Equal(AuthorizationRules.Revoked, AuthorizationRules.IsValidAt(revoked, At(Wednesday, "10:00")).Reason);
            Assert.Equal(AuthorizationRules.AlreadyUsed, AuthorizationRules.IsValidAt(consumed, At(Wednesday, "10:00")).Reason);
        }

        [Fact]
        public void No_vale_si_quien_autorizo_ya_no_esta_vinculado_a_la_unidad()
        {
            var validity = AuthorizationRules.IsValidAt(Authorization(), At(Wednesday, "10:00"), authorizerStillLinked: false);

            Assert.Equal(AuthorizationRules.AuthorizerUnlinked, validity.Reason);
        }

        [Fact]
        public void La_franja_horaria_incluye_sus_extremos()
        {
            var authorization = Authorization(from: "08:00", to: "12:00");

            Assert.Equal(AuthorizationRules.OutsideHours, AuthorizationRules.IsValidAt(authorization, At(Wednesday, "07:59")).Reason);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "08:00")).IsValid);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "12:00")).IsValid);
            Assert.Equal(AuthorizationRules.OutsideHours, AuthorizationRules.IsValidAt(authorization, At(Wednesday, "12:01")).Reason);
        }

        [Fact]
        public void La_recurrente_solo_vale_los_dias_de_la_semana_indicados()
        {
            var authorization = Authorization(AuthorizationKind.Recurring, Wednesday, Wednesday.AddDays(30),
                days: new[] { DayOfWeek.Monday, DayOfWeek.Wednesday });

            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "10:00")).IsValid);
            Assert.Equal(AuthorizationRules.WrongDay, AuthorizationRules.IsValidAt(authorization, At(Wednesday.AddDays(1), "10:00")).Reason);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday.AddDays(5), "10:00")).IsValid); // lunes
        }

        [Fact]
        public void Una_franja_que_cruza_la_medianoche_pertenece_al_dia_en_que_empieza()
        {
            // turno de noche de los miércoles, 22:00 a 02:00
            var authorization = Authorization(AuthorizationKind.Recurring, Wednesday, Wednesday.AddDays(30), "22:00", "02:00",
                DayOfWeek.Wednesday);
            var thursday = Wednesday.AddDays(1);

            Assert.Equal(AuthorizationRules.OutsideHours, AuthorizationRules.IsValidAt(authorization, At(Wednesday, "21:59")).Reason);
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(Wednesday, "23:30")).IsValid);
            // la madrugada del jueves sigue siendo el turno del miércoles
            Assert.True(AuthorizationRules.IsValidAt(authorization, At(thursday, "01:00")).IsValid);
            Assert.Equal(AuthorizationRules.WrongDay, AuthorizationRules.IsValidAt(authorization, At(thursday, "03:00")).Reason);
            // la madrugada del miércoles pertenecería al martes, que no está autorizado
            Assert.Equal(AuthorizationRules.NotYet, AuthorizationRules.IsValidAt(authorization, At(Wednesday, "01:00")).Reason);
        }

        [Fact]
        public void El_estado_que_se_muestra_distingue_vencida_y_por_empezar()
        {
            var authorization = Authorization(start: Wednesday, end: Wednesday.AddDays(1));

            Assert.Equal("NotStarted", AuthorizationRules.StateOf(authorization, Wednesday.AddDays(-1)));
            Assert.Equal("Active", AuthorizationRules.StateOf(authorization, Wednesday));
            Assert.Equal("Expired", AuthorizationRules.StateOf(authorization, Wednesday.AddDays(2)));
        }
    }
}
