using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Application.Access
{
    /// <summary>Veredicto de una autorización en un instante: permite ingresar o no, y por qué.</summary>
    public readonly record struct Validity(bool IsValid, string? Reason)
    {
        public static readonly Validity Ok = new(true, null);
        public static Validity No(string reason) => new(false, reason);
    }

    /// <summary>
    /// Reglas de vigencia de una autorización de visita. Es una función pura sobre la hora local
    /// del conjunto: la misma que usa la pantalla de portería para mostrar el veredicto y el
    /// servidor para decidir al registrar el ingreso.
    /// </summary>
    public static class AuthorizationRules
    {
        public const string Revoked = "La autorización fue revocada.";
        public const string AlreadyUsed = "La autorización era para un solo ingreso y ya se usó.";
        public const string NotYet = "La autorización todavía no empieza.";
        public const string Expired = "La autorización está vencida.";
        public const string WrongDay = "La autorización no aplica para hoy.";
        public const string OutsideHours = "La autorización no aplica a esta hora.";
        public const string AuthorizerUnlinked = "Quien autorizó ya no está vinculado a la unidad.";

        /// <param name="local">Fecha y hora en la zona horaria del conjunto.</param>
        /// <param name="authorizerStillLinked">Quien autorizó conserva un vínculo vigente con la unidad.</param>
        public static Validity IsValidAt(VisitAuthorization authorization, DateTime local, bool authorizerStillLinked = true)
        {
            if (authorization.Status == AuthorizationStatus.Revoked) return Validity.No(Revoked);
            if (authorization.Status == AuthorizationStatus.Consumed) return Validity.No(AlreadyUsed);
            if (!authorizerStillLinked) return Validity.No(AuthorizerUnlinked);

            var today = DateOnly.FromDateTime(local);
            var time = TimeOnly.FromDateTime(local);
            var crossesMidnight = authorization.TimeFrom.HasValue && authorization.TimeTo.HasValue
                && authorization.TimeFrom.Value > authorization.TimeTo.Value;

            // Una franja que cruza la medianoche (22:00–02:00) pertenece al día en que empieza:
            // a la 01:00 se evalúa el día anterior.
            var day = crossesMidnight && time <= authorization.TimeTo!.Value ? today.AddDays(-1) : today;

            if (day < authorization.StartDate)
            {
                return Validity.No(NotYet);
            }
            if (day > authorization.EndDate)
            {
                return Validity.No(Expired);
            }
            if (authorization.Kind == AuthorizationKind.Recurring && (authorization.DaysOfWeek & (1 << (int)day.DayOfWeek)) == 0)
            {
                return Validity.No(WrongDay);
            }
            if (!IsWithinHours(authorization, time, crossesMidnight))
            {
                return Validity.No(OutsideHours);
            }

            return Validity.Ok;
        }

        /// <summary>Aplica algún día dentro de su rango: aún puede usarse hoy o más adelante.</summary>
        public static bool CouldApplyOn(VisitAuthorization authorization, DateOnly day)
        {
            if (authorization.Status != AuthorizationStatus.Active) return false;
            if (day < authorization.StartDate || day > authorization.EndDate) return false;
            return authorization.Kind != AuthorizationKind.Recurring
                || (authorization.DaysOfWeek & (1 << (int)day.DayOfWeek)) != 0;
        }

        /// <summary>Estado para mostrar: los guardados más los que se calculan por fecha.</summary>
        public static string StateOf(VisitAuthorization authorization, DateOnly today)
        {
            return authorization.Status switch
            {
                AuthorizationStatus.Revoked => "Revoked",
                AuthorizationStatus.Consumed => "Consumed",
                _ when today > authorization.EndDate => "Expired",
                _ when today < authorization.StartDate => "NotStarted",
                _ => "Active"
            };
        }

        private static bool IsWithinHours(VisitAuthorization authorization, TimeOnly time, bool crossesMidnight)
        {
            var from = authorization.TimeFrom;
            var to = authorization.TimeTo;

            if (from == null && to == null) return true;
            if (crossesMidnight) return time >= from!.Value || time <= to!.Value;
            return (from == null || time >= from.Value) && (to == null || time <= to.Value);
        }

        public static int ToMask(System.Collections.Generic.IEnumerable<int> days)
        {
            var mask = 0;
            foreach (var day in days)
            {
                if (day is >= 0 and <= 6) mask |= 1 << day;
            }
            return mask;
        }

        public static System.Collections.Generic.List<int> FromMask(int mask)
        {
            var days = new System.Collections.Generic.List<int>();
            for (var day = 0; day <= 6; day++)
            {
                if ((mask & (1 << day)) != 0) days.Add(day);
            }
            return days;
        }
    }
}
