using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Common.DTOs.Vehicles;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class VehicleTests(ApiFixture api)
    {
        private sealed record Scene(HttpClient Admin, HttpClient Guard, Guid ComplexId, UnitDto Unit,
            Guid ResidentPersonId, HttpClient Resident, Guid OccupancyId);

        /// <summary>Conjunto propio con una unidad, un arrendatario con cuenta y un guarda.</summary>
        private async Task<Scene> SceneAsync()
        {
            var (admin, complex, _) = await TestData.CreateComplexAsync(api, "Vehiculos");
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin, "Rosa", "Residente");
            var occupancy = await TestData.AddOccupantAsync(admin, unit.Id, person.Id, OccupancyRelation.Tenant);
            var (_, resident) = await TestData.GrantAccessAsync(api, admin, person.Id, SystemRoles.Resident);

            var guardEmail = $"guard-{Guid.NewGuid():N}@test.com";
            await (await admin.PostAsJsonAsync("/api/users", new
            {
                email = guardEmail, firstName = "Gil", lastName = "Guarda", roleKeys = new[] { SystemRoles.Guard }
            })).ReadDataAsync<UserDto>();

            return new Scene(admin, await api.ClientForAsync(guardEmail, TestData.Password), complex.Id, unit, person.Id, resident, occupancy.Id);
        }

        private static string Plate() => "T" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

        private static Task<HttpResponseMessage> CreateAsync(HttpClient client, Guid unitId, Guid? responsibleId, string? plate, string type = "automovil", string path = "/api/vehicles") =>
            client.PostAsJsonAsync(path, new { unitId, responsiblePersonId = responsibleId, plate, type, brand = "Mazda", color = "Rojo" });

        private static async Task<PlateCheckDto> CheckAsync(HttpClient client, string plate) =>
            await (await client.GetAsync($"/api/vehicles/by-plate/{plate}")).ReadDataAsync<PlateCheckDto>();

        // ---------- HU-124: inventario ----------

        [Fact]
        public async Task La_placa_se_normaliza_y_no_se_repite_y_la_consulta_por_placa_dice_a_quien_pertenece()
        {
            var scene = await SceneAsync();
            var plate = Plate();
            var spaced = $"{plate[..3]} {plate[3..]}".ToLowerInvariant();

            var car = await (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, spaced)).ReadDataAsync<VehicleDto>();
            Assert.Equal(plate, car.Plate);
            Assert.Equal(VehicleClass.Permanent, car.Class);
            Assert.Equal("Rosa Residente", car.ResponsibleName);
            Assert.True(car.IsAuthorizedToday);

            // la misma placa escrita de otra forma es el mismo vehículo
            var duplicate = await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, $"{plate[..3]}-{plate[3..]}");
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            // una bicicleta puede no tener placa; un automóvil no
            var bike = await (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, null, "bicicleta")).ReadDataAsync<VehicleDto>();
            Assert.False(bike.HasPlate);
            Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, null)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, Plate(), "nave")).StatusCode);

            // el responsable debe tener vínculo vigente con la unidad
            var stranger = await TestData.CreatePersonAsync(scene.Admin);
            Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(scene.Admin, scene.Unit.Id, stranger.Id, Plate())).StatusCode);

            var check = await CheckAsync(scene.Guard, spaced.Replace(" ", "-"));
            Assert.Equal(PlateCheckResult.Authorized, check.Result);
            Assert.Equal(scene.Unit.Label, check.Vehicle!.UnitLabel);
            Assert.Contains(check.Drivers, d => d.PersonId == scene.ResidentPersonId);
            Assert.Equal(PlateCheckResult.Unknown, (await CheckAsync(scene.Guard, "ZZZ999")).Result);

            // filtros del inventario y sección de la ficha de unidad
            var byPlate = await (await scene.Guard.GetAsync($"/api/vehicles?search={plate[..4]}")).ReadDataAsync<PaginatedResultDto<VehicleDto>>();
            Assert.Equal(car.Id, Assert.Single(byPlate.Items!).Id);
            var bikes = await (await scene.Admin.GetAsync($"/api/vehicles?type=bicicleta&unitId={scene.Unit.Id}")).ReadDataAsync<PaginatedResultDto<VehicleDto>>();
            Assert.Equal(bike.Id, Assert.Single(bikes.Items!).Id);
            var summary = await (await scene.Guard.GetAsync($"/api/units/{scene.Unit.Id}/summary")).ReadDataAsync<UnitSummaryDto>();
            Assert.Equal(2, summary.Vehicles.Count);

            // portería consulta, no registra
            Assert.Equal(HttpStatusCode.Forbidden, (await CreateAsync(scene.Guard, scene.Unit.Id, scene.ResidentPersonId, Plate())).StatusCode);
        }

        [Fact]
        public async Task Suspender_revocar_y_reasignar_conservan_el_historial_y_exigen_motivo()
        {
            var scene = await SceneAsync();
            var plate = Plate();
            var car = await (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, plate)).ReadDataAsync<VehicleDto>();

            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/suspend", new { reason = "" })).StatusCode);
            var suspended = await (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/suspend", new { reason = "Mal parqueado tres veces" })).ReadDataAsync<VehicleDto>();
            Assert.Equal(VehicleAuthorizationStatus.Suspended, suspended.Status);
            Assert.False(suspended.IsAuthorizedToday);
            var check = await CheckAsync(scene.Guard, plate);
            Assert.Equal(PlateCheckResult.NotValid, check.Result);
            Assert.Equal("Autorización suspendida.", check.Message);

            (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/reactivate", new { })).EnsureSuccessStatusCode();
            Assert.Equal(PlateCheckResult.Authorized, (await CheckAsync(scene.Guard, plate)).Result);

            // pasa a otra unidad con otro responsable: la anterior se cierra, no se pierde
            var other = await TestData.CreateOwnerWithUnitAsync(api, scene.Admin);
            var moved = await (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/reassign", new { unitId = other.UnitId, responsiblePersonId = other.PersonId })).ReadDataAsync<VehicleDto>();
            Assert.Equal(other.UnitId, moved.UnitId);
            Assert.Equal("Olga Propietaria", moved.ResponsibleName);

            var detail = await (await scene.Admin.GetAsync($"/api/vehicles/{car.Id}")).ReadDataAsync<VehicleDetailDto>();
            Assert.Equal(2, detail.History.Count);
            Assert.Equal(VehicleAuthorizationStatus.Active, detail.History[0].Status);
            Assert.Equal(VehicleAuthorizationStatus.Ended, detail.History[1].Status);
            Assert.Contains(detail.Notes, n => n.Text.StartsWith("Suspendido") && n.AuthorName == "Ana Admin");

            // la base impide dos autorizaciones en curso del mismo vehículo, venga de donde venga
            var duplicated = await Assert.ThrowsAnyAsync<Exception>(() => api.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"VehicleAuthorizations\" (\"Id\", \"ComplexId\", \"VehicleId\", \"UnitId\", \"ResponsiblePersonId\", \"StartDate\", \"Status\", \"CreatedAt\", \"IsActive\") " +
                "SELECT gen_random_uuid(), \"ComplexId\", \"VehicleId\", \"UnitId\", \"ResponsiblePersonId\", \"StartDate\", 1, now(), true FROM \"VehicleAuthorizations\" WHERE \"Status\" = 1 AND \"VehicleId\" = {0}", car.Id), scene.ComplexId));
            Assert.Contains("UX_VehicleAuthorizations_Open", duplicated.ToString());

            var revoked = await (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/revoke", new { reason = "Vendió el vehículo" })).ReadDataAsync<VehicleDto>();
            Assert.Equal(VehicleAuthorizationStatus.Revoked, revoked.Status);
            Assert.Equal(PlateCheckResult.NotValid, (await CheckAsync(scene.Guard, plate)).Result);
            var revokedList = await (await scene.Admin.GetAsync("/api/vehicles?status=Revoked")).ReadDataAsync<PaginatedResultDto<VehicleDto>>();
            Assert.Contains(revokedList.Items!, v => v.Id == car.Id);

            // la misma placa puede volver a registrarse después
            Assert.Equal(HttpStatusCode.OK, (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, plate)).StatusCode);
        }

        [Fact]
        public async Task Al_terminar_la_ocupacion_del_responsable_su_vehiculo_queda_revocado()
        {
            var scene = await SceneAsync();
            var plate = Plate();
            await (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, plate)).ReadDataAsync<VehicleDto>();

            (await scene.Admin.PostAsJsonAsync($"/api/occupancies/{scene.OccupancyId}/end", new { endDate = TestData.Today.AddDays(-1) })).EnsureSuccessStatusCode();

            var check = await CheckAsync(scene.Guard, plate);
            Assert.Equal(PlateCheckResult.NotValid, check.Result);
            Assert.Equal("Vinculación del responsable finalizada", check.Vehicle!.StatusReason);
        }

        [Fact]
        public async Task El_maximo_de_vehiculos_por_unidad_se_respeta()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.MaxVehiclesPerUnit, "1")).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, Plate())).StatusCode);
            var second = await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, Plate());
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            Assert.Contains("máximo de vehículos", (await second.ReadErrorAsync()).Message);
        }

        [Fact]
        public async Task El_residente_registra_sus_vehiculos_segun_la_politica_del_conjunto()
        {
            var scene = await SceneAsync();
            var other = await TestData.CreateUnitAsync(scene.Admin);
            Task<HttpResponseMessage> MineAsync(Guid unitId, string plate) => CreateAsync(scene.Resident, unitId, null, plate, path: "/api/my/vehicles");

            // por defecto: con aprobación de la administración
            var plate = Plate();
            var requested = await (await MineAsync(scene.Unit.Id, plate)).ReadDataAsync<VehicleDto>();
            Assert.Equal(VehicleAuthorizationStatus.Pending, requested.Status);
            Assert.Equal("Rosa Residente", requested.ResponsibleName);
            Assert.Equal(PlateCheckResult.NotValid, (await CheckAsync(scene.Guard, plate)).Result);

            var mine = await scene.Resident.GetAsync("/api/my/vehicles");
            Assert.Equal(ComplexPolicies.WithApproval, (await mine.Content.ReadFromJsonAsync<AccessControl.Common.DTOs.ResponseModel<List<VehicleDto>>>())!.Message);

            var pending = await (await scene.Admin.GetAsync("/api/vehicles/pending")).ReadDataAsync<List<VehicleDto>>();
            Assert.Equal(requested.Id, Assert.Single(pending).Id);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{requested.Id}/decide", new { approve = false })).StatusCode);
            (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{requested.Id}/decide", new { approve = true })).EnsureSuccessStatusCode();
            Assert.Equal(PlateCheckResult.Authorized, (await CheckAsync(scene.Guard, plate)).Result);

            // una unidad ajena responde como inexistente
            Assert.Equal(HttpStatusCode.NotFound, (await MineAsync(other.Id, Plate())).StatusCode);

            // registro directo
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.ResidentRegistersVehicles, ComplexPolicies.Direct)).EnsureSuccessStatusCode();
            var direct = await (await MineAsync(scene.Unit.Id, Plate())).ReadDataAsync<VehicleDto>();
            Assert.Equal(VehicleAuthorizationStatus.Active, direct.Status);

            // lo retira el propio residente
            var removed = await (await scene.Resident.PostAsJsonAsync($"/api/my/vehicles/{direct.Id}/revoke", new { })).ReadDataAsync<VehicleDto>();
            Assert.Equal(VehicleAuthorizationStatus.Revoked, removed.Status);

            // no permitido
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.ResidentRegistersVehicles, ComplexPolicies.No)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await MineAsync(scene.Unit.Id, Plate())).StatusCode);
        }

        // ---------- HU-125: vehículos en el acceso ----------

        [Fact]
        public async Task El_vehiculo_de_una_visita_queda_enlazado_y_su_presencia_se_calcula_de_los_eventos()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var plate = Plate();

            var visit = await (await scene.Guard.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = scene.Unit.Id, vehiclePlate = plate.ToLowerInvariant(), vehicleType = "motocicleta", vehicleColor = "Negro",
                newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).ReadDataAsync<VisitDto>();
            Assert.Equal(plate, visit.VehiclePlate);
            Assert.NotNull(visit.VehicleId);

            var check = await CheckAsync(scene.Guard, plate);
            Assert.Equal(PlateCheckResult.VisitInProgress, check.Result);
            Assert.Equal("Vera Visitante", check.VisitorName);
            Assert.True(check.IsInside);
            Assert.Equal(VehicleClass.Visitor, check.Vehicle!.Class);
            Assert.Equal("motocicleta", check.Vehicle.Type);

            var inside = await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>();
            Assert.Equal(plate, Assert.Single(inside).Plate);

            // el historial filtra por la placa en cualquier formato
            var byPlate = await (await scene.Guard.GetAsync($"/api/visits?plate={plate[..3]}-{plate[3..]}")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visit.Id, Assert.Single(byPlate.Items!).Id);

            // el visitante sale sin el vehículo: sigue dentro y la visita lo avisa
            var left = await (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { withoutVehicle = true })).ReadDataAsync<VisitDto>();
            Assert.True(left.VehicleStillInside);
            var still = Assert.Single(await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>());
            Assert.True(still.VisitorLeft);

            var exit = await (await scene.Guard.PostAsJsonAsync($"/api/access/vehicles/{visit.VehicleId}/exit", new { notes = "Lo retiró en grúa" })).ReadDataAsync<AccessEventDto>();
            Assert.Equal(AccessEventType.Exit, exit.Type);
            Assert.Equal(plate, exit.VehiclePlate);
            Assert.Empty(await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>());
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Guard.PostAsJsonAsync($"/api/access/vehicles/{visit.VehicleId}/exit", new { })).StatusCode);

            // segunda visita con la misma placa: reutiliza el vehículo y la salida normal lo saca
            var again = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, plate);
            Assert.Equal(visit.VehicleId, again.VehicleId);
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{again.Id}/check-out", new { })).EnsureSuccessStatusCode();
            Assert.Empty(await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>());
            Assert.Equal(PlateCheckResult.Unknown, (await CheckAsync(scene.Guard, plate)).Result);
        }

        [Fact]
        public async Task Un_vehiculo_permanente_solo_ingresa_con_autorizacion_vigente_o_como_excepcion()
        {
            var scene = await SceneAsync();
            var plate = Plate();
            var car = await (await CreateAsync(scene.Admin, scene.Unit.Id, scene.ResidentPersonId, plate)).ReadDataAsync<VehicleDto>();
            object Movement(AccessEventType type, object? extra = null)
            {
                var body = new Dictionary<string, object?> { ["personId"] = scene.ResidentPersonId, ["unitId"] = scene.Unit.Id, ["type"] = type, ["vehicleId"] = car.Id };
                foreach (var property in extra?.GetType().GetProperties() ?? [])
                {
                    body[property.Name] = property.GetValue(extra);
                }
                return body;
            }

            var entry = await (await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry))).ReadDataAsync<AccessEventDto>();
            Assert.Equal(plate, entry.VehiclePlate);
            Assert.Equal(car.Id, entry.VehicleId);
            Assert.True((await CheckAsync(scene.Guard, plate)).IsInside);
            var inside = Assert.Single(await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>());
            Assert.Equal(VehicleClass.Permanent, inside.Class);

            (await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Exit))).EnsureSuccessStatusCode();
            Assert.False((await CheckAsync(scene.Guard, plate)).IsInside);

            // suspendido: no entra de forma normal
            (await scene.Admin.PostAsJsonAsync($"/api/vehicles/{car.Id}/suspend", new { reason = "Deuda de administración" })).EnsureSuccessStatusCode();
            var blocked = await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry));
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

            // la excepción exige permiso y motivo
            var exception = new { isException = true, exceptionReason = "Autorizó la administradora por teléfono" };
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry, exception))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry, new { isException = true, exceptionReason = "sí" }))).StatusCode);
            var allowed = await (await scene.Admin.PostAsJsonAsync("/api/access/residents", Movement(AccessEventType.Entry, exception))).ReadDataAsync<AccessEventDto>();
            Assert.Equal(VerificationResult.Exception, allowed.Verification);

            // una placa que no existe no entra como vehículo de residente
            var unknown = await scene.Guard.PostAsJsonAsync("/api/access/residents", new { personId = scene.ResidentPersonId, unitId = scene.Unit.Id, type = AccessEventType.Entry, vehiclePlate = "NOEXISTE" });
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        }

        [Fact]
        public async Task Anular_la_entrada_de_un_vehiculo_lo_saca_de_los_presentes()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var visit = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, Plate());

            (await scene.Admin.PostAsJsonAsync($"/api/access/events/{visit.EntryEventId}/void", new { reason = "Placa mal digitada" })).EnsureSuccessStatusCode();

            Assert.Empty(await (await scene.Guard.GetAsync("/api/vehicles/inside")).ReadDataAsync<List<VehicleInsideDto>>());
        }
    }
}
