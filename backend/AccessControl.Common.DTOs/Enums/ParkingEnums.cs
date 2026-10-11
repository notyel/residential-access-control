namespace AccessControl.Shared.Enums
{
    /// <summary>A quién está destinado el espacio. Es un dato del espacio, no su estado.</summary>
    public enum ParkingSpaceType
    {
        Private = 0,
        Community = 1,
        Visitor = 2
    }

    public enum ParkingVehicleKind
    {
        Any = 0,
        Car = 1,
        Motorcycle = 2
    }

    public enum ParkingAssignmentClass
    {
        /// <summary>Unidad titular de un espacio privado.</summary>
        Holder = 0,
        /// <summary>Uso temporal de un comunitario por una unidad o un vehículo.</summary>
        Temporary = 1,
        Reservation = 2,
        /// <summary>Un vehículo está estacionado en el espacio.</summary>
        Occupancy = 3
    }

    public enum ParkingAssignmentStatus
    {
        Active = 0,
        /// <summary>Terminó de forma normal: el vehículo salió, la titularidad cambió o la reserva se usó.</summary>
        Released = 1,
        Cancelled = 2
    }

    /// <summary>Estado operativo. No se guarda: se calcula de las asignaciones en el momento de consultar.</summary>
    public enum ParkingState
    {
        Available = 0,
        Occupied = 1,
        Reserved = 2,
        OutOfService = 3
    }
}
