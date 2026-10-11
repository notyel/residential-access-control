namespace AccessControl.Shared.Enums
{
    public enum OccupancyRelation
    {
        /// <summary>Propietario que vive en su unidad; exige una titularidad vigente.</summary>
        OwnerResident = 0,
        Tenant = 1,
        Family = 2,
        Other = 3
    }

    public enum OccupancyStatus
    {
        /// <summary>Solicitada por un propietario; espera la aprobación de la administración.</summary>
        Pending = 0,
        Active = 1,
        Ended = 2,
        Rejected = 3
    }

    /// <summary>Estado de ocupación de una unidad; se calcula, no se guarda.</summary>
    public enum UnitOccupancyState
    {
        Vacant = 0,
        OwnerOccupied = 1,
        Rented = 2
    }
}
