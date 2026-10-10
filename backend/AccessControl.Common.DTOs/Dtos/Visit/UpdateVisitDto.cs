using System.ComponentModel.DataAnnotations;

namespace AccessControl.Shared.Dtos.Visit
{
    // Lo único corregible de una visita registrada es la placa, y solo mientras no tenga salida
    public class UpdateVisitDto
    {
        [MaxLength(15)]
        public string? VehiclePlate { get; set; }
    }
}
