namespace AccessControl.Shared.Dtos.Visit
{
    public class UpdateVisitDto
    {
        public string VisitorName { get; set; } = null!;
        public string VisitorId { get; set; } = null!;
        public string? VehiclePlate { get; set; }
    }
}
