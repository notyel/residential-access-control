using AccessControl.Application.Parking;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Parking;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Common.DTOs.Vehicles;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class ParkingTests(ApiFixture api)
    {
        private sealed record Scene(HttpClient Admin, HttpClient Guard, Guid ComplexId, UnitDto Unit, Guid ResidentPersonId, HttpClient Resident);

        /// <summary>Conjunto propio (visitas a criterio de portería) con una unidad, un arrendatario con cuenta y un guarda.</summary>
        private async Task<Scene> SceneAsync()
        {
            var (admin, complex, _) = await TestData.CreateComplexAsync(api, "Parqueo");
            await TestData.AllowWalkInsAsync(admin);
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin, "Rosa", "Residente");
            await TestData.AddOccupantAsync(admin, unit.Id, person.Id, OccupancyRelation.Tenant);
            var (_, resident) = await TestData.GrantAccessAsync(api, admin, person.Id, SystemRoles.Resident);

            var guardEmail = $"guard-{Guid.NewGuid():N}@test.com";
            await (await admin.PostAsJsonAsync("/api/users", new
            {
                email = guardEmail, firstName = "Gil", lastName = "Guarda", roleKeys = new[] { SystemRoles.Guard }
            })).ReadDataAsync<UserDto>();

            return new Scene(admin, await api.ClientForAsync(guardEmail, TestData.Password), complex.Id, unit, person.Id, resident);
        }

        private static string Plate() => "P" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

        private static async Task<ParkingSpaceDto> SpaceAsync(HttpClient admin, string code, ParkingSpaceType type, ParkingVehicleKind kind = ParkingVehicleKind.Any) =>
            await (await admin.PostAsJsonAsync("/api/parking/spaces", new { code, type, vehicleKind = kind })).ReadDataAsync<ParkingSpaceDto>();

        private static Task<HttpResponseMessage> CheckInAsync(HttpClient guard, Guid unitId, string plate, Guid? spaceId, string vehicleType = "automovil") =>
            guard.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId, vehiclePlate = plate, vehicleType, parkingSpaceId = spaceId,
                newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            });

        private static async Task<AvailabilityRowDto> RowAsync(HttpClient client, ParkingSpaceType type)
        {
            var availability = await (await client.GetAsync("/api/parking/availability")).ReadDataAsync<AvailabilityDto>();
            return availability.ByType.Single(r => r.Type == type);
        }

        private static async Task<ParkingSpaceDto> StateAsync(HttpClient client, Guid spaceId) =>
            (await (await client.GetAsync($"/api/parking/spaces/{spaceId}")).ReadDataAsync<ParkingSpaceDetailDto>()).Space;

        // ---------- HU-126: registro ----------

        [Fact]
        public async Task Los_espacios_tienen_codigo_unico_se_crean_en_lote_y_un_privado_conserva_el_historial_de_sus_titulares()
        {
            var scene = await SceneAsync();
            var privateSpace = await SpaceAsync(scene.Admin, "p-01", ParkingSpaceType.Private);
            Assert.Equal("P-01", privateSpace.Code);
            Assert.Equal(ParkingState.Available, privateSpace.State);
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PostAsJsonAsync("/api/parking/spaces", new { code = "P-01", type = ParkingSpaceType.Visitor })).StatusCode);

            await SpaceAsync(scene.Admin, "V-02", ParkingSpaceType.Visitor);
            var bulk = await (await scene.Admin.PostAsJsonAsync("/api/parking/spaces/bulk", new { prefix = "V-", from = 1, to = 5, type = ParkingSpaceType.Visitor })).ReadDataAsync<BulkParkingResultDto>();
            Assert.Equal(new[] { "V-01", "V-03", "V-04", "V-05" }, bulk.Created);
            Assert.Equal(new[] { "V-02" }, bulk.Skipped);

            // un privado se asocia a su unidad; reasignarlo cierra la asociación anterior
            var other = await TestData.CreateOwnerWithUnitAsync(api, scene.Admin);
            var held = await (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/holder", new { unitId = scene.Unit.Id, personId = scene.ResidentPersonId })).ReadDataAsync<ParkingSpaceDto>();
            Assert.Equal(scene.Unit.Label, held.HolderUnitLabel);
            Assert.Equal("Rosa Residente", held.HolderPersonName);
            (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/holder", new { unitId = other.UnitId })).EnsureSuccessStatusCode();

            var detail = await (await scene.Admin.GetAsync($"/api/parking/spaces/{privateSpace.Id}")).ReadDataAsync<ParkingSpaceDetailDto>();
            var holders = detail.History.Where(h => h.Class == ParkingAssignmentClass.Holder).ToList();
            Assert.Equal(2, holders.Count);
            Assert.Contains(holders, h => h.Status == ParkingAssignmentStatus.Released && h.UnitLabel == scene.Unit.Label && h.EndsAt != null);
            Assert.Contains(holders, h => h.Status == ParkingAssignmentStatus.Active && h.RegisteredBy == "Ana Admin");

            // solo los privados tienen titular, y no cambian de tipo mientras lo tengan
            var visitorSpace = (await (await scene.Admin.GetAsync("/api/parking/spaces?type=Visitor&search=V-01")).ReadDataAsync<List<ParkingSpaceDto>>()).Single();
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{visitorSpace.Id}/holder", new { unitId = scene.Unit.Id })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PutAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}", new { code = "P-01", type = ParkingSpaceType.Visitor })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/deactivate", new { })).StatusCode);

            // un privado nunca se ofrece a los visitantes
            var offered = await (await scene.Guard.GetAsync("/api/parking/visitor-spaces")).ReadDataAsync<List<ParkingSpaceDto>>();
            Assert.Equal(5, offered.Count);
            Assert.All(offered, s => Assert.Equal(ParkingSpaceType.Visitor, s.Type));

            // ficha de la unidad y portal del propietario
            var summary = await (await scene.Admin.GetAsync($"/api/units/{other.UnitId}/summary")).ReadDataAsync<UnitSummaryDto>();
            Assert.Equal("P-01", Assert.Single(summary.Parking).Code);
            Assert.Equal("P-01", Assert.Single(await (await other.Client.GetAsync("/api/my/parking")).ReadDataAsync<List<ParkingSpaceDto>>()).Code);
            Assert.Empty(await (await scene.Resident.GetAsync("/api/my/parking")).ReadDataAsync<List<ParkingSpaceDto>>());

            // dar de baja no borra: deja de contar, conserva el historial
            (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{visitorSpace.Id}/deactivate", new { })).EnsureSuccessStatusCode();
            Assert.Equal(4, (await RowAsync(scene.Admin, ParkingSpaceType.Visitor)).Capacity);
            Assert.Contains(await (await scene.Admin.GetAsync("/api/parking/spaces?includeInactive=true")).ReadDataAsync<List<ParkingSpaceDto>>(), s => s.Id == visitorSpace.Id && !s.IsActive);

            // portería consulta; no crea. El portal no ve el inventario del conjunto
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync("/api/parking/spaces", new { code = "X-1", type = ParkingSpaceType.Visitor })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Resident.GetAsync("/api/parking/spaces")).StatusCode);
        }

        // ---------- HU-127 y HU-128: ocupación, liberación y disponibilidad calculada ----------

        [Fact]
        public async Task La_disponibilidad_se_calcula_de_las_asignaciones_y_cambia_sola_con_cada_ingreso_y_salida()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.CommunityParkingReservations, "true")).EnsureSuccessStatusCode();
            await scene.Admin.PostAsJsonAsync("/api/parking/spaces/bulk", new { prefix = "V-", from = 1, to = 5, type = ParkingSpaceType.Visitor });
            var community = await SpaceAsync(scene.Admin, "C-01", ParkingSpaceType.Community);
            var spaces = await (await scene.Guard.GetAsync("/api/parking/visitor-spaces")).ReadDataAsync<List<ParkingSpaceDto>>();

            // dos visitas con vehículo ocupan dos espacios
            var first = await (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), spaces[0].Id)).ReadDataAsync<VisitDto>();
            var secondPlate = Plate();
            await (await CheckInAsync(scene.Guard, scene.Unit.Id, secondPlate, spaces[1].Id)).ReadDataAsync<VisitDto>();
            Assert.Equal("V-01", first.ParkingSpaceCode);
            // uno fuera de servicio y una reserva en el comunitario
            (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{spaces[2].Id}/out-of-service", new { reason = "Filtración de agua" })).EnsureSuccessStatusCode();
            (await scene.Admin.PostAsJsonAsync("/api/parking/reservations", new
            {
                spaceId = community.Id, unitId = scene.Unit.Id, startsAt = DateTime.UtcNow.AddMinutes(1), endsAt = DateTime.UtcNow.AddHours(3)
            })).EnsureSuccessStatusCode();

            var visitors = await RowAsync(scene.Guard, ParkingSpaceType.Visitor);
            Assert.Equal((5, 2, 0, 2, 1), (visitors.Capacity, visitors.Occupied, visitors.Reserved, visitors.Available, visitors.OutOfService));
            var availability = await (await scene.Guard.GetAsync("/api/parking/availability")).ReadDataAsync<AvailabilityDto>();
            Assert.Equal(6, availability.Total.Capacity);
            Assert.Equal(availability.Total.Capacity, availability.Total.Occupied + availability.Total.Reserved + availability.Total.Available + availability.Total.OutOfService);
            // la reserva empieza en un minuto: cuenta para el periodo futuro, aún no para "ahora"
            var future = await (await scene.Guard.GetAsync($"/api/parking/availability?from={DateTime.UtcNow.AddMinutes(30):O}&to={DateTime.UtcNow.AddHours(1):O}")).ReadDataAsync<AvailabilityDto>();
            Assert.Equal(1, future.ByType.Single(r => r.Type == ParkingSpaceType.Community).Reserved);

            // quién ocupa el espacio
            var occupied = await StateAsync(scene.Guard, spaces[1].Id);
            Assert.Equal(ParkingState.Occupied, occupied.State);
            Assert.Equal(secondPlate, occupied.Occupant!.Plate);
            Assert.Equal(scene.Unit.Label, occupied.Occupant.UnitLabel);
            Assert.DoesNotContain(await (await scene.Guard.GetAsync("/api/parking/visitor-spaces")).ReadDataAsync<List<ParkingSpaceDto>>(), s => s.Id == spaces[1].Id || s.Id == spaces[2].Id);

            // la salida de la visita libera su espacio sin ninguna otra acción
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{first.Id}/check-out", new { })).EnsureSuccessStatusCode();
            visitors = await RowAsync(scene.Guard, ParkingSpaceType.Visitor);
            Assert.Equal((1, 3, 1), (visitors.Occupied, visitors.Available, visitors.OutOfService));
            var history = (await (await scene.Admin.GetAsync($"/api/parking/spaces/{spaces[0].Id}")).ReadDataAsync<ParkingSpaceDetailDto>()).History;
            Assert.Equal(ParkingAssignmentStatus.Released, Assert.Single(history).Status);

            // al terminar el periodo fuera de servicio el espacio vuelve solo a disponible
            var broken = await StateAsync(scene.Admin, spaces[2].Id);
            Assert.Equal("Filtración de agua", broken.OutOfServiceReason);
            Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), spaces[2].Id)).StatusCode);
            (await scene.Admin.PostAsJsonAsync($"/api/parking/out-of-service/{broken.OutOfServiceId}/end", new { })).EnsureSuccessStatusCode();
            Assert.Equal(4, (await RowAsync(scene.Guard, ParkingSpaceType.Visitor)).Available);

            // una ocupación que supera el tiempo máximo se marca, no se libera sola
            await api.WithDbAsync(db => db.ParkingAssignments.Where(a => a.SpaceId == spaces[1].Id && a.Status == ParkingAssignmentStatus.Active)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.StartsAt, DateTime.UtcNow.AddHours(-13))), scene.ComplexId);
            var overdue = await StateAsync(scene.Guard, spaces[1].Id);
            Assert.True(overdue.Occupant!.IsOverdue);
            Assert.Equal(ParkingState.Occupied, overdue.State);
        }

        [Fact]
        public async Task Dos_ingresos_simultaneos_al_mismo_espacio_no_pueden_ganar_ambos()
        {
            var scene = await SceneAsync();
            var space = await SpaceAsync(scene.Admin, "V-01", ParkingSpaceType.Visitor);

            var responses = await Task.WhenAll(
                CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), space.Id),
                CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), space.Id));

            Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, responses.Select(r => r.StatusCode).OrderBy(s => s));
            // el ingreso que perdió no dejó una visita a medias
            var open = await api.WithDbAsync(db => db.Visits.CountAsync(v => v.ExitEventId == null), scene.ComplexId);
            Assert.Equal(1, open);

            // y la propia base rechaza el cruce, venga de donde venga
            var direct = await Assert.ThrowsAnyAsync<Exception>(() => api.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"ParkingAssignments\" (\"Id\", \"ComplexId\", \"SpaceId\", \"Class\", \"StartsAt\", \"Status\", \"IsException\", \"CreatedAt\", \"IsActive\") " +
                "SELECT gen_random_uuid(), \"ComplexId\", \"Id\", 3, now(), 0, false, now(), true FROM \"ParkingSpaces\" WHERE \"Id\" = {0}", space.Id), scene.ComplexId));
            Assert.Contains("EX_ParkingAssignments_Occupancy", direct.ToString());
        }

        [Fact]
        public async Task Un_espacio_no_admite_vehiculos_de_otro_tipo_ni_un_vehiculo_dos_espacios_y_un_privado_es_de_su_unidad()
        {
            var scene = await SceneAsync();
            var motorcycles = await SpaceAsync(scene.Admin, "M-01", ParkingSpaceType.Visitor, ParkingVehicleKind.Motorcycle);
            var visitorA = await SpaceAsync(scene.Admin, "V-01", ParkingSpaceType.Visitor);
            var visitorB = await SpaceAsync(scene.Admin, "V-02", ParkingSpaceType.Visitor);
            var privateSpace = await SpaceAsync(scene.Admin, "P-01", ParkingSpaceType.Private);
            (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/holder", new { unitId = scene.Unit.Id })).EnsureSuccessStatusCode();

            // un carro no cabe en un espacio de motos; una moto sí
            Assert.Equal(HttpStatusCode.BadRequest, (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), motorcycles.Id)).StatusCode);
            Assert.DoesNotContain(await (await scene.Guard.GetAsync("/api/parking/visitor-spaces?vehicleType=automovil")).ReadDataAsync<List<ParkingSpaceDto>>(), s => s.Id == motorcycles.Id);
            Assert.Equal(HttpStatusCode.OK, (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), motorcycles.Id, "motocicleta")).StatusCode);

            // el mismo vehículo no ocupa dos espacios
            var plate = Plate();
            (await CheckInAsync(scene.Guard, scene.Unit.Id, plate, visitorA.Id)).EnsureSuccessStatusCode();
            var twice = await scene.Guard.PostAsJsonAsync($"/api/parking/spaces/{visitorB.Id}/occupy", new { plate });
            Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
            Assert.Equal(ParkingService.OverlapMessage, (await twice.ReadErrorAsync()).Message);

            // un privado solo recibe vehículos de su unidad titular: para otro, excepción con permiso y motivo
            var visitorPlate = Plate();
            Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(scene.Guard, scene.Unit.Id, visitorPlate, privateSpace.Id)).StatusCode);
            var visit = await (await CheckInAsync(scene.Guard, scene.Unit.Id, visitorPlate, null)).ReadDataAsync<VisitDto>();
            object Exception(string reason) => new { plate = visitorPlate, isException = true, exceptionReason = reason };
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/occupy", Exception("El residente lo pidió por citófono"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/occupy", Exception("ok"))).StatusCode);
            var exceptional = await (await scene.Admin.PostAsJsonAsync($"/api/parking/spaces/{privateSpace.Id}/occupy", Exception("El residente lo pidió por citófono"))).ReadDataAsync<ParkingSpaceDto>();
            Assert.True(exceptional.Occupant!.IsException);
            Assert.Equal(visit.Id, exceptional.Occupant.VisitId);
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).EnsureSuccessStatusCode();
            Assert.Equal(ParkingState.Available, (await StateAsync(scene.Admin, privateSpace.Id)).State);

            // el vehículo del residente entra a su privado sin excepción y lo libera al salir
            var car = await (await scene.Admin.PostAsJsonAsync("/api/vehicles", new { unitId = scene.Unit.Id, responsiblePersonId = scene.ResidentPersonId, plate = Plate(), type = "automovil" })).ReadDataAsync<VehicleDto>();
            object Movement(AccessEventType type) => new { personId = scene.ResidentPersonId, unitId = scene.Unit.Id, type, vehicleId = car.Id, parkingSpaceId = type == AccessEventType.Entry ? privateSpace.Id : (Guid?)null };
            (await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry))).EnsureSuccessStatusCode();
            var parked = await StateAsync(scene.Admin, privateSpace.Id);
            Assert.Equal(car.Plate, parked.Occupant!.Plate);
            Assert.False(parked.Occupant.IsException);
            (await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Exit))).EnsureSuccessStatusCode();
            Assert.Equal(ParkingState.Available, (await StateAsync(scene.Admin, privateSpace.Id)).State);

            // anular una entrada registrada por error también deja libre el espacio
            var mistaken = await (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), visitorB.Id)).ReadDataAsync<VisitDto>();
            (await scene.Admin.PostAsJsonAsync($"/api/access/events/{mistaken.EntryEventId}/void", new { reason = "Se registró en la unidad equivocada" })).EnsureSuccessStatusCode();
            Assert.Equal(ParkingState.Available, (await StateAsync(scene.Admin, visitorB.Id)).State);
        }

        [Fact]
        public async Task Las_asignaciones_temporales_y_las_reservas_no_se_cruzan_y_una_reserva_sin_usar_se_libera_sola()
        {
            var scene = await SceneAsync();
            var other = await TestData.CreateUnitAsync(scene.Admin);
            var community = await SpaceAsync(scene.Admin, "C-01", ParkingSpaceType.Community);
            var second = await SpaceAsync(scene.Admin, "C-02", ParkingSpaceType.Community);
            var visitor = await SpaceAsync(scene.Admin, "V-01", ParkingSpaceType.Visitor);
            var now = DateTime.UtcNow;
            object Plan(Guid spaceId, Guid unitId, double fromHours, double toHours) => new { spaceId, unitId, startsAt = now.AddHours(fromHours), endsAt = now.AddHours(toHours) };

            // asignación temporal: inicio y fin obligatorios, solo en comunitarios
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync("/api/parking/assignments", new { spaceId = community.Id, unitId = scene.Unit.Id, startsAt = now })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync("/api/parking/assignments", Plan(visitor.Id, scene.Unit.Id, 0, 2))).StatusCode);
            var assigned = await (await scene.Admin.PostAsJsonAsync("/api/parking/assignments", Plan(community.Id, scene.Unit.Id, 0, 4))).ReadDataAsync<ParkingSpaceDto>();
            Assert.Equal(ParkingState.Reserved, assigned.State);
            Assert.Equal(ParkingAssignmentClass.Temporary, assigned.Plan!.Class);

            // otra que se cruce en el mismo espacio: 409; una posterior: sí
            var overlap = await scene.Admin.PostAsJsonAsync("/api/parking/assignments", Plan(community.Id, other.Id, 2, 6));
            Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await scene.Admin.PostAsJsonAsync("/api/parking/assignments", Plan(community.Id, other.Id, 4, 6))).StatusCode);

            // un vehículo de otra unidad no ocupa lo que está apartado; el de la unidad sí
            var foreignPlate = Plate();
            Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(scene.Guard, other.Id, foreignPlate, community.Id)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), community.Id)).StatusCode);

            // reservas: solo si la política las habilita
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Admin.PostAsJsonAsync("/api/parking/reservations", Plan(second.Id, scene.Unit.Id, 1, 3))).StatusCode);
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.CommunityParkingReservations, "true")).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync("/api/parking/reservations", Plan(second.Id, scene.Unit.Id, -2, 3))).StatusCode);
            var reserved = await (await scene.Admin.PostAsJsonAsync("/api/parking/reservations", Plan(second.Id, scene.Unit.Id, 0.01, 3))).ReadDataAsync<ParkingSpaceDto>();
            var reservationId = reserved.Plan?.AssignmentId
                ?? (await (await scene.Admin.GetAsync($"/api/parking/spaces/{second.Id}")).ReadDataAsync<ParkingSpaceDetailDto>()).History.Single().Id;
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync("/api/parking/reservations", Plan(second.Id, scene.Unit.Id, 5, 6))).StatusCode);

            // la reserva empezó hace 10 minutos: el vehículo de la unidad la convierte en ocupación
            await api.WithDbAsync(db => db.ParkingAssignments.Where(a => a.Id == reservationId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.StartsAt, DateTime.UtcNow.AddMinutes(-10))), scene.ComplexId);
            Assert.Equal(ParkingState.Reserved, (await StateAsync(scene.Admin, second.Id)).State);
            var visit = await (await CheckInAsync(scene.Guard, scene.Unit.Id, Plate(), second.Id)).ReadDataAsync<VisitDto>();
            var used = (await (await scene.Admin.GetAsync($"/api/parking/spaces/{second.Id}")).ReadDataAsync<ParkingSpaceDetailDto>());
            Assert.Equal(ParkingState.Occupied, used.Space.State);
            Assert.Equal(ParkingAssignmentStatus.Released, used.History.Single(h => h.Id == reservationId).Status);
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).EnsureSuccessStatusCode();

            // otra reserva que nadie usa: pasado el margen deja de apartar el espacio y el proceso la cancela
            var unused = await (await scene.Admin.PostAsJsonAsync("/api/parking/reservations", Plan(second.Id, scene.Unit.Id, 0.01, 5))).ReadDataAsync<ParkingSpaceDto>();
            var unusedId = (await (await scene.Admin.GetAsync($"/api/parking/spaces/{second.Id}")).ReadDataAsync<ParkingSpaceDetailDto>())
                .History.Single(h => h.Class == ParkingAssignmentClass.Reservation && h.Status == ParkingAssignmentStatus.Active).Id;
            await api.WithDbAsync(db => db.ParkingAssignments.Where(a => a.Id == unusedId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.StartsAt, DateTime.UtcNow.AddMinutes(-90))), scene.ComplexId);
            Assert.Equal(ParkingState.Available, (await StateAsync(scene.Admin, second.Id)).State);

            using (var scope = api.Factory.Services.CreateScope())
            {
                Assert.True(await scope.ServiceProvider.GetRequiredService<IParkingService>().CancelUnusedReservationsAsync() >= 1);
            }
            var cancelled = (await (await scene.Admin.GetAsync($"/api/parking/spaces/{second.Id}")).ReadDataAsync<ParkingSpaceDetailDto>()).History.Single(h => h.Id == unusedId);
            Assert.Equal(ParkingAssignmentStatus.Cancelled, cancelled.Status);
            Assert.NotNull(unused);
        }
    }
}
