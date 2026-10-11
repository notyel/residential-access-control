using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessControl.Application.Policies
{
    public enum PolicyKind
    {
        Boolean,
        Number,
        Choice
    }

    public sealed record PolicyOption(string Value, string Label);

    /// <param name="Options">Valores admitidos cuando Kind es Choice.</param>
    /// <param name="Min">Mínimo admitido cuando Kind es Number.</param>
    /// <param name="Max">Máximo admitido cuando Kind es Number.</param>
    public sealed record PolicyDefinition(
        string Key,
        string Group,
        string Label,
        string Description,
        PolicyKind Kind,
        string DefaultValue,
        IReadOnlyList<PolicyOption>? Options = null,
        int Min = 0,
        int Max = int.MaxValue);

    /// <summary>
    /// Reglas de operación que cada conjunto ajusta sin cambiar el programa. El catálogo vive en
    /// código; en la base solo se guardan los valores que un conjunto cambió respecto al valor
    /// por defecto, así una política nueva rige de inmediato en todos los conjuntos.
    /// </summary>
    public static class ComplexPolicies
    {
        public const string MultipleOwners = "units.multiple_owners";
        public const string OwnerManagesOccupants = "occupants.owner_manages";
        public const string ResidentCanAuthorize = "visits.resident_can_authorize";
        public const string VisitsWithoutAuthorization = "visits.without_authorization";
        public const string VisitorDocumentRequired = "visits.document_required";
        public const string AuthorizationMaxDays = "visits.authorization_max_days";
        public const string VisitReviewAfterHours = "visits.review_after_hours";
        public const string ResidentRegistersVehicles = "vehicles.resident_registers";
        public const string MaxVehiclesPerUnit = "vehicles.max_per_unit";
        public const string VisitorParkingMaxHours = "parking.visitor_max_hours";
        public const string CommunityParkingReservations = "parking.community_reservations";
        public const string SessionMaxHours = "session.max_hours";
        public const string AccessHistoryDays = "access.history_days";
        public const string VisitorRetentionDays = "privacy.visitor_retention_days";

        // Valores de las políticas de tipo "no / con aprobación / directo"
        public const string No = "no";
        public const string WithApproval = "approval";
        public const string Direct = "direct";

        // Valores de visits.without_authorization
        public const string NotAllowed = "not_allowed";
        public const string ResidentConfirmation = "resident_confirmation";
        public const string GuardDiscretion = "guard_discretion";

        private static readonly IReadOnlyList<PolicyOption> SelfService = new List<PolicyOption>
        {
            new(No, "No; solo la administración"),
            new(WithApproval, "Sí, con aprobación de la administración"),
            new(Direct, "Sí, directamente"),
        };

        public static readonly IReadOnlyList<PolicyDefinition> All = new List<PolicyDefinition>
        {
            new(MultipleOwners, "Unidades y ocupantes", "Varios propietarios por unidad",
                "Permite registrar más de un propietario vigente en la misma unidad.",
                PolicyKind.Boolean, "true"),
            new(OwnerManagesOccupants, "Unidades y ocupantes", "El propietario gestiona sus ocupantes",
                "Si el propietario puede registrar y actualizar a quienes viven en sus unidades.",
                PolicyKind.Choice, WithApproval, SelfService),

            new(ResidentCanAuthorize, "Visitantes", "El residente puede autorizar visitas",
                "Si no, solo el propietario y la administración crean autorizaciones.",
                PolicyKind.Boolean, "true"),
            new(VisitsWithoutAuthorization, "Visitantes", "Visitas sin autorización previa",
                "Qué hace portería cuando llega alguien que nadie autorizó de antemano.",
                PolicyKind.Choice, ResidentConfirmation, new List<PolicyOption>
                {
                    new(NotAllowed, "No se permiten"),
                    new(ResidentConfirmation, "Con confirmación de un residente de la unidad"),
                    new(GuardDiscretion, "A criterio de portería"),
                }),
            new(VisitorDocumentRequired, "Visitantes", "Documento del visitante obligatorio",
                "Exige el número de documento para registrar un ingreso.",
                PolicyKind.Boolean, "true"),
            new(AuthorizationMaxDays, "Visitantes", "Duración máxima de una autorización (días)",
                "Ninguna autorización de visita puede durar más que esto.",
                PolicyKind.Number, "30", Min: 1, Max: 365),
            new(VisitReviewAfterHours, "Visitantes", "Horas para marcar una visita sin salida como \"por revisar\"",
                "Una visita que lleva más tiempo dentro se destaca para que portería la revise.",
                PolicyKind.Number, "24", Min: 1, Max: 720),

            new(ResidentRegistersVehicles, "Vehículos", "El residente registra sus vehículos",
                "Si propietarios y residentes pueden registrar los vehículos de su unidad.",
                PolicyKind.Choice, WithApproval, SelfService),
            new(MaxVehiclesPerUnit, "Vehículos", "Vehículos máximos por unidad",
                "0 significa sin límite.",
                PolicyKind.Number, "0", Min: 0, Max: 100),

            new(VisitorParkingMaxHours, "Parqueaderos", "Tiempo máximo en parqueadero de visitantes (horas)",
                "Pasado este tiempo la ocupación se marca como excedida.",
                PolicyKind.Number, "12", Min: 1, Max: 720),
            new(CommunityParkingReservations, "Parqueaderos", "Reservas de parqueadero comunitario",
                "Permite reservar un parqueadero comunitario para una franja futura.",
                PolicyKind.Boolean, "false"),

            new(AccessHistoryDays, "Seguridad", "Días de historial de accesos para portería",
                "Quien consulta el historial sin permiso de reportes solo ve los últimos días.",
                PolicyKind.Number, "7", Min: 1, Max: 365),
            new(VisitorRetentionDays, "Seguridad", "Retención de datos de visitantes (días)",
                "Pasado este tiempo desde su último acceso, el visitante se anonimiza. 0 significa conservar siempre.",
                PolicyKind.Number, "0", Min: 0, Max: 3650),
            new(SessionMaxHours, "Seguridad", "Duración máxima de sesión (horas)",
                "Tiempo tras el cual hay que volver a iniciar sesión.",
                PolicyKind.Number, "12", Min: 1, Max: 720),
        };

        public static PolicyDefinition? Find(string key) => All.FirstOrDefault(p => p.Key == key);

        /// <summary>Devuelve el valor normalizado, o null si no es válido para esa política.</summary>
        public static string? Normalize(PolicyDefinition definition, string? value)
        {
            var text = (value ?? string.Empty).Trim().ToLowerInvariant();
            switch (definition.Kind)
            {
                case PolicyKind.Boolean:
                    return text is "true" or "false" ? text : null;
                case PolicyKind.Number:
                    return int.TryParse(text, out var number) && number >= definition.Min && number <= definition.Max
                        ? number.ToString()
                        : null;
                default:
                    return definition.Options!.Any(o => o.Value == text) ? text : null;
            }
        }
    }
}
