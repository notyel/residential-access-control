namespace AccessControl.Shared.Enums
{
    public enum VehicleClass
    {
        /// <summary>De un propietario u ocupante: tiene autorización asociada a una unidad.</summary>
        Permanent = 0,
        /// <summary>Entró con una visita; no tiene autorización permanente.</summary>
        Visitor = 1
    }

    public enum VehicleAuthorizationStatus
    {
        /// <summary>Solicitada desde el portal, a la espera de la administración.</summary>
        Pending = 0,
        Active = 1,
        Suspended = 2,
        Revoked = 3,
        /// <summary>Cerrada porque el vehículo pasó a otra unidad o a otro responsable.</summary>
        Ended = 4,
        Rejected = 5
    }

    /// <summary>Resultado de verificar una placa en portería.</summary>
    public enum PlateCheckResult
    {
        Authorized = 0,
        /// <summary>Registrado, pero su autorización está vencida, suspendida, revocada o pendiente.</summary>
        NotValid = 1,
        /// <summary>Vehículo de una visita que sigue dentro.</summary>
        VisitInProgress = 2,
        Unknown = 3
    }
}
