namespace AccessControl.Shared.Enums
{
    public enum AccessEventType
    {
        Entry = 0,
        Exit = 1,
        Rejection = 2,
        /// <summary>Anula otro evento registrado por error.</summary>
        Void = 3
    }

    public enum PersonKind
    {
        Visitor = 0,
        Resident = 1,
        Owner = 2
    }

    /// <summary>Cómo se verificó el ingreso de un visitante.</summary>
    public enum VerificationResult
    {
        PriorAuthorization = 0,
        ResidentConfirmation = 1,
        GuardApproval = 2,
        /// <summary>Acceso excepcional: sin autorización válida, con motivo y responsable.</summary>
        Exception = 3
    }

    public enum AuthorizationKind
    {
        /// <summary>Un ingreso en una fecha.</summary>
        Single = 0,
        /// <summary>Cualquier número de ingresos entre dos fechas.</summary>
        Period = 1,
        /// <summary>Ciertos días de la semana y franja horaria, entre dos fechas.</summary>
        Recurring = 2
    }

    public enum AuthorizationStatus
    {
        Active = 0,
        Revoked = 1,
        /// <summary>Autorización única ya usada.</summary>
        Consumed = 2
    }
}
