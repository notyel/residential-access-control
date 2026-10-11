using AccessControl.Application.Parking;
using AccessControl.Shared.Enums;

namespace AccessControl.Tests.Unit
{
    public class ParkingRulesTests
    {
        [Theory]
        // fuera de servicio manda sobre todo lo demás
        [InlineData(true, true, true, ParkingState.OutOfService)]
        [InlineData(true, false, false, ParkingState.OutOfService)]
        // ocupado manda sobre reservado
        [InlineData(false, true, true, ParkingState.Occupied)]
        [InlineData(false, true, false, ParkingState.Occupied)]
        [InlineData(false, false, true, ParkingState.Reserved)]
        [InlineData(false, false, false, ParkingState.Available)]
        public void El_estado_sigue_la_precedencia_fuera_de_servicio_ocupado_reservado_disponible(
            bool outOfService, bool occupied, bool planned, ParkingState expected)
        {
            Assert.Equal(expected, ParkingRules.ComputeState(outOfService, occupied, planned));
        }

        [Theory]
        [InlineData(ParkingVehicleKind.Any, "bicicleta", true)]
        [InlineData(ParkingVehicleKind.Car, "automovil", true)]
        [InlineData(ParkingVehicleKind.Car, "motocicleta", false)]
        [InlineData(ParkingVehicleKind.Motorcycle, "motocicleta", true)]
        [InlineData(ParkingVehicleKind.Motorcycle, "automovil", false)]
        public void Un_espacio_solo_admite_el_tipo_de_vehiculo_para_el_que_esta_hecho(ParkingVehicleKind kind, string vehicleType, bool expected)
        {
            Assert.Equal(expected, ParkingRules.Accepts(kind, vehicleType));
        }
    }
}
