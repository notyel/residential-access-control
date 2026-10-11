using AccessControl.Application.Policies;
using AccessControl.Application.Reports;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Report;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class ReportTests(ApiFixture api)
    {
        private sealed record Scene(HttpClient Admin, HttpClient Guard, HttpClient Auditor, Guid ComplexId, UnitDto Unit, Guid ResidentPersonId, HttpClient Resident);

        private async Task<Scene> SceneAsync()
        {
            var (admin, complex, _) = await TestData.CreateComplexAsync(api, "Reportes");
            await TestData.AllowWalkInsAsync(admin);
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin, "Rosa", "Residente");
            await TestData.AddOccupantAsync(admin, unit.Id, person.Id, OccupancyRelation.Tenant, TestData.Today.AddDays(-2), TestData.Today.AddDays(10));
            var (_, resident) = await TestData.GrantAccessAsync(api, admin, person.Id, SystemRoles.Resident);
            var guard = await TestData.CreateUserAsync(api, admin, SystemRoles.Guard);
            var auditor = await TestData.CreateUserAsync(api, admin, SystemRoles.Auditor, "Aura", "Auditora");
            return new Scene(admin, guard, auditor, complex.Id, unit, person.Id, resident);
        }

        private static string Range(int daysBack = 1) =>
            $"startDate={DateTime.UtcNow.AddDays(-daysBack):O}&endDate={DateTime.UtcNow.AddMinutes(5):O}";

        private static async Task<List<AccessEventDto>> HistoryAsync(HttpClient client, string query = "") =>
            (await (await client.GetAsync($"/api/access-history?{Range()}&pageSize=100{query}")).ReadDataAsync<PaginatedResultDto<AccessEventDto>>()).Items!.ToList();

        // ---------- HU-129: historial ----------

        [Fact]
        public async Task El_historial_reune_todos_los_eventos_se_filtra_y_respeta_lo_que_cada_rol_puede_ver()
        {
            var scene = await SceneAsync();
            var other = await TestData.CreateUnitAsync(scene.Admin);
            var visit = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, "abc 901", "Zulma");
            var elsewhere = await TestData.CheckInAsync(scene.Guard, other.Id, firstName: "Yolanda");
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).EnsureSuccessStatusCode();
            (await scene.Guard.PostAsJsonAsync("/api/access/reject", new { unitId = scene.Unit.Id, personName = "Desconocido", reasonCode = "sin-autorizacion" })).EnsureSuccessStatusCode();
            (await scene.Guard.PostAsJsonAsync("/api/access/residents", new { personId = scene.ResidentPersonId, unitId = scene.Unit.Id, type = AccessEventType.Entry })).EnsureSuccessStatusCode();
            (await scene.Admin.PostAsJsonAsync($"/api/access/events/{elsewhere.EntryEventId}/void", new { reason = "Unidad equivocada" })).EnsureSuccessStatusCode();

            // entradas, salida, rechazo, residente y anulación: todo está
            var all = await HistoryAsync(scene.Admin);
            Assert.Equal(6, all.Count);
            Assert.Contains(all, e => e.Type == AccessEventType.Void);
            Assert.Contains(all, e => e.Type == AccessEventType.Entry && e.IsVoided);

            // filtros combinables
            Assert.Equal(2, (await HistoryAsync(scene.Admin, $"&unitId={scene.Unit.Id}&plate=ABC-901")).Count);
            Assert.Single(await HistoryAsync(scene.Admin, "&type=Rejection"));
            Assert.Single(await HistoryAsync(scene.Admin, "&personKind=Resident"));
            Assert.Equal(2, (await HistoryAsync(scene.Admin, "&search=zulma")).Count);

            // el rango de fechas es obligatorio y tiene un máximo
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.GetAsync("/api/access-history")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.GetAsync($"/api/access-history?{Range(400)}")).StatusCode);

            // el residente solo ve su unidad, con el documento enmascarado
            var mine = await HistoryAsync(scene.Resident);
            Assert.Equal(4, mine.Count);
            Assert.All(mine, e => Assert.Equal(scene.Unit.Label, e.UnitLabel));
            Assert.All(mine.Where(e => e.PersonDocument != null), e => Assert.Contains(PersonalDataPolicy.Mask, e.PersonDocument));

            // … y solo desde que empezó su vínculo: un evento anterior no aparece
            await api.WithDbAsync(db => db.Occupancies.Where(o => o.PersonId == scene.ResidentPersonId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.StartDate, TestData.Today.AddDays(2))), scene.ComplexId);
            Assert.Empty(await HistoryAsync(scene.Resident));

            // portería solo ve los últimos días que el conjunto define
            Assert.Equal(6, (await HistoryAsync(scene.Guard)).Count);
            await api.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"AccessEvents\" DISABLE TRIGGER trg_access_events_immutable; " +
                "UPDATE \"AccessEvents\" SET \"OccurredAt\" = now() - interval '20 days' WHERE \"ComplexId\" = {0} AND \"Type\" = 2; " +
                "ALTER TABLE \"AccessEvents\" ENABLE TRIGGER trg_access_events_immutable;", scene.ComplexId), scene.ComplexId);
            var wide = $"/api/access-history?startDate={DateTime.UtcNow.AddDays(-30):O}&endDate={DateTime.UtcNow.AddMinutes(5):O}&pageSize=100";
            Assert.Equal(5, (await (await scene.Guard.GetAsync(wide)).ReadDataAsync<PaginatedResultDto<AccessEventDto>>()).TotalCount);
            Assert.Equal(6, (await (await scene.Auditor.GetAsync(wide)).ReadDataAsync<PaginatedResultDto<AccessEventDto>>()).TotalCount);
        }

        // ---------- HU-129: reportes, exportación y novedades ----------

        [Fact]
        public async Task Los_reportes_se_consultan_y_exportan_con_permiso_y_la_exportacion_queda_en_la_bitacora()
        {
            var scene = await SceneAsync();
            var visit = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, "rep 001");
            (await scene.Admin.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = scene.Unit.Id, isException = true, exceptionReason = "Emergencia médica en la unidad",
                newPerson = new { firstName = "Elsa", lastName = "Excepción", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).EnsureSuccessStatusCode();
            (await scene.Guard.PostAsJsonAsync("/api/access/reject", new { unitId = scene.Unit.Id, personName = "Rechazado", reasonCode = "no-confirmado", vehiclePlate = "zzz 111" })).EnsureSuccessStatusCode();
            await scene.Admin.PostAsJsonAsync("/api/parking/spaces/bulk", new { prefix = "V-", from = 1, to = 3, type = ParkingSpaceType.Visitor });

            var definitions = await (await scene.Auditor.GetAsync("/api/reports")).ReadDataAsync<List<ReportDefinitionDto>>();
            Assert.Equal(6, definitions.Count);
            async Task<ReportDto> RunAsync(string key) => await (await scene.Auditor.GetAsync($"/api/reports/{key}?{Range()}")).ReadDataAsync<ReportDto>();

            var byDay = Assert.Single((await RunAsync(ReportingService.AccessesByDay)).Rows);
            Assert.Equal(new[] { "2", "0", "1", "1" }, byDay.Skip(1));
            var byUnit = Assert.Single((await RunAsync(ReportingService.VisitsByUnit)).Rows);
            Assert.Equal(new[] { scene.Unit.Label, "2", "1", "2" }, byUnit);
            var alerts = (await RunAsync(ReportingService.RejectionsAndExceptions)).Rows;
            Assert.Equal(2, alerts.Count);
            Assert.Contains(alerts, r => r[1] == "Acceso excepcional" && r[5] == "Emergencia médica en la unidad" && r[6] == "Ana Admin");
            Assert.Equal(new[] { "Visitantes", "3", "0", "0", "3", "0" }, (await RunAsync(ReportingService.ParkingOccupancy)).Rows.Single(r => r[0] == "Visitantes"));
            Assert.Equal("ZZZ111", Assert.Single((await RunAsync(ReportingService.UnauthorizedVehicles)).Rows)[1]);
            // la ocupación del residente termina en 10 días
            Assert.Equal(new[] { "Ocupación", scene.Unit.Label, "Rosa Residente" }, Assert.Single((await RunAsync(ReportingService.ExpiringLinks)).Rows).Take(3));
            Assert.Equal(HttpStatusCode.NotFound, (await scene.Auditor.GetAsync($"/api/reports/inventado?{Range()}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Auditor.GetAsync($"/api/reports/{ReportingService.VisitsByUnit}")).StatusCode);

            // exportación: CSV con los mismos datos
            var export = await scene.Auditor.GetAsync($"/api/reports/{ReportingService.VisitsByUnit}/export?{Range()}");
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            Assert.Equal("text/csv", export.Content.Headers.ContentType!.MediaType);
            var csv = await export.Content.ReadAsStringAsync();
            Assert.Contains("Unidad;Visitas;Con vehículo;Siguen dentro", csv);
            Assert.Contains($"{scene.Unit.Label};2;1;2", csv);

            // el historial exportado respeta el enmascarado de quien exporta (el auditor no ve documentos)
            var history = await (await scene.Auditor.GetAsync($"/api/access-history/export?{Range()}")).Content.ReadAsStringAsync();
            Assert.Contains("Elsa Excepción", history);
            Assert.DoesNotContain(visit.Person.DocumentNumber, history);
            Assert.Contains(PersonalDataPolicy.Mask, history);

            // queda en la bitácora con los filtros usados
            var audit = await (await scene.Admin.GetAsync("/api/audit?action=report.exported")).ReadDataAsync<PaginatedResultDto<AuditLogDto>>();
            Assert.Equal(2, audit.TotalCount);
            Assert.Contains(audit.Items!, a => a.EntityId == ReportingService.VisitsByUnit && a.UserName == "Aura Auditora" && a.Changes!.Contains("StartDate"));

            // portería no ve reportes ni exporta; el auditor consulta y exporta pero no escribe
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.GetAsync("/api/reports")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.GetAsync($"/api/access-history/export?{Range()}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Auditor.PostAsJsonAsync("/api/units", new { code = "X-1", type = "apartamento" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Auditor.PostAsJsonAsync("/api/incidents", new { category = "seguridad", text = "No debería poder" })).StatusCode);
        }

        [Fact]
        public async Task Las_novedades_del_turno_solo_se_agregan_y_se_consultan_por_categoria()
        {
            var scene = await SceneAsync();
            var created = await (await scene.Guard.PostAsJsonAsync("/api/incidents", new
            {
                category = "seguridad", text = "Puerta del parqueadero quedó abierta", unitId = scene.Unit.Id, vehiclePlate = "abc-123"
            })).ReadDataAsync<IncidentDto>();
            Assert.Equal("Seguridad", created.CategoryLabel);
            Assert.Equal("Gil Guarda", created.AuthorName);
            Assert.Equal("ABC123", created.VehiclePlate);
            (await scene.Guard.PostAsJsonAsync("/api/incidents", new { category = "mantenimiento", text = "Bombillo fundido en la portería" })).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Guard.PostAsJsonAsync("/api/incidents", new { category = "inventada", text = "Categoría que no existe" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Guard.PostAsJsonAsync("/api/incidents", new { category = "seguridad", text = "no" })).StatusCode);

            var security = await (await scene.Auditor.GetAsync("/api/incidents?category=seguridad")).ReadDataAsync<PaginatedResultDto<IncidentDto>>();
            Assert.Equal(created.Id, Assert.Single(security.Items!).Id);
            Assert.Equal(2, (await (await scene.Admin.GetAsync("/api/incidents")).ReadDataAsync<PaginatedResultDto<IncidentDto>>()).TotalCount);

            // no hay forma de editarlas ni borrarlas, y el portal no las ve
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await scene.Admin.PutAsJsonAsync("/api/incidents", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await scene.Admin.DeleteAsync($"/api/incidents/{created.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Resident.GetAsync("/api/incidents")).StatusCode);
        }

        // ---------- HU-130: panel por bloques ----------

        [Fact]
        public async Task El_panel_entrega_a_cada_rol_solo_sus_bloques()
        {
            var scene = await SceneAsync();
            await scene.Admin.PostAsJsonAsync("/api/parking/spaces/bulk", new { prefix = "V-", from = 1, to = 2, type = ParkingSpaceType.Visitor });
            await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, "pan 001");
            (await scene.Guard.PostAsJsonAsync("/api/access/reject", new { unitId = scene.Unit.Id, personName = "Rechazado", reasonCode = "otro" })).EnsureSuccessStatusCode();
            (await scene.Guard.PostAsJsonAsync("/api/incidents", new { category = "otro", text = "Cambio de turno sin novedades" })).EnsureSuccessStatusCode();
            (await scene.Resident.PostAsJsonAsync("/api/my/authorizations", new { unitId = scene.Unit.Id, visitorName = "Tía Marta", kind = AuthorizationKind.Single, startDate = TestData.Today })).EnsureSuccessStatusCode();
            async Task<DashboardDto> PanelAsync(HttpClient client) => await (await client.GetAsync("/api/dashboard")).ReadDataAsync<DashboardDto>();

            var guard = await PanelAsync(scene.Guard);
            Assert.NotNull(guard.Gate);
            Assert.Null(guard.Management);
            Assert.Null(guard.Portal);
            Assert.Equal((1, 1, 1), (guard.Gate!.ExpectedToday, guard.Gate.Inside, guard.Gate.VehiclesInside));
            Assert.Equal((2, 2), (guard.Gate.VisitorParking.Capacity, guard.Gate.VisitorParking.Available));
            Assert.Equal("Cambio de turno sin novedades", Assert.Single(guard.Gate.LatestIncidents).Text);

            // el administrador también opera portería: ve la unión de bloques
            var admin = await PanelAsync(scene.Admin);
            Assert.NotNull(admin.Gate);
            Assert.Null(admin.Portal);
            var management = admin.Management!;
            Assert.Equal((0, 0, 1), (management.UnitsVacant, management.UnitsOwnerOccupied, management.UnitsRented));
            Assert.Equal((1, 1, 1), (management.AccessesToday, management.AccessesWeek, management.Inside));
            Assert.Equal("rejection", Assert.Single(management.RecentAlerts).Kind);
            Assert.Equal(1, management.ExpiringLinks);
            Assert.Equal(3, management.Parking.Count);

            // el auditor ve los indicadores, sin el bloque de operación
            var auditor = await PanelAsync(scene.Auditor);
            Assert.NotNull(auditor.Management);
            Assert.Null(auditor.Gate);

            var resident = await PanelAsync(scene.Resident);
            Assert.Null(resident.Gate);
            Assert.Null(resident.Management);
            Assert.Equal(scene.Unit.Label, Assert.Single(resident.Portal!.Units));
            Assert.Equal(1, resident.Portal.ActiveAuthorizations);
            Assert.Single(resident.LatestVisits!);
        }

        // ---------- HU-131: datos personales ----------

        [Fact]
        public async Task El_documento_y_el_contacto_solo_salen_completos_para_quien_tiene_permiso()
        {
            var scene = await SceneAsync();
            var document = HttpExtensions.UniqueDocument();
            var person = await (await scene.Admin.PostAsJsonAsync("/api/persons", new
            {
                firstName = "Dora", lastName = "Datos", documentType = "CC", documentNumber = document, phone = "3001234567", email = "dora@test.com"
            })).ReadDataAsync<PersonDto>();
            await TestData.AddOwnerAsync(scene.Admin, scene.Unit.Id, person.Id);
            var masked = PersonalDataPolicy.Mask + document[^4..];

            // administración: completo
            var forAdmin = (await (await scene.Admin.GetAsync("/api/persons/search?search=Dora")).ReadDataAsync<PaginatedResultDto<PersonListItemDto>>()).Items!.Single();
            Assert.Equal((document, "3001234567", "dora@test.com"), (forAdmin.DocumentNumber, forAdmin.Phone, forAdmin.Email));

            // portería en listas y fichas: enmascarado y sin contacto. El servidor nunca envía el valor
            var list = await scene.Guard.GetAsync("/api/persons/search?search=Dora");
            Assert.DoesNotContain(document, await list.Content.ReadAsStringAsync());
            var forGuard = (await list.ReadDataAsync<PaginatedResultDto<PersonListItemDto>>()).Items!.Single();
            Assert.Equal((masked, null, null), (forGuard.DocumentNumber, forGuard.Phone, forGuard.Email));
            var detail = await scene.Guard.GetAsync($"/api/persons/{person.Id}");
            Assert.DoesNotContain(document, await detail.Content.ReadAsStringAsync());

            // … pero al validar un ingreso (búsqueda exacta por documento) sí lo ve completo
            var lookup = await (await scene.Guard.GetAsync($"/api/access/lookup?term={document}")).ReadDataAsync<LookupResultDto>();
            Assert.Equal(document, Assert.Single(lookup.People).DocumentNumber);

            // historiales: enmascarado para portería y para el residente
            var visit = await (await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, personId = person.Id })).ReadDataAsync<VisitDto>();
            foreach (var client in new[] { scene.Guard, scene.Resident, scene.Auditor })
            {
                var body = await (await client.GetAsync("/api/visits")).Content.ReadAsStringAsync();
                Assert.DoesNotContain(document, body);
                Assert.Contains(masked, body);
            }
            Assert.Equal(document, (await (await scene.Admin.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>()).Person.DocumentNumber);

            // quien solo ve el documento enmascarado no lo daña al editar la persona
            (await scene.Guard.PutAsJsonAsync($"/api/persons/{person.Id}", new { firstName = "Dora", lastName = "Datos Díaz", documentType = "CC", documentNumber = masked })).EnsureSuccessStatusCode();
            var afterEdit = await (await scene.Admin.GetAsync($"/api/persons/{person.Id}")).ReadDataAsync<PersonDetailDto>();
            Assert.Equal((document, "Datos Díaz"), (afterEdit.Person.DocumentNumber, afterEdit.Person.LastName));

            // ver el documento completo desde una ficha queda en la bitácora
            var audit = await (await scene.Admin.GetAsync("/api/audit?action=person.identity_viewed")).ReadDataAsync<PaginatedResultDto<AuditLogDto>>();
            Assert.Contains(audit.Items!, a => a.EntityId == person.Id.ToString() && a.UserName == "Ana Admin");
            Assert.DoesNotContain(audit.Items!, a => a.UserName == "Gil Guarda");
        }

        [Fact]
        public async Task Pasado_el_periodo_de_retencion_el_visitante_se_anonimiza_y_sus_accesos_se_conservan()
        {
            var scene = await SceneAsync();
            var old = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, firstName: "Antiguo");
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{old.Id}/check-out", new { })).EnsureSuccessStatusCode();
            var recent = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, firstName: "Reciente");
            async Task<int> RunAsync()
            {
                using var scope = api.Factory.Services.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IPrivacyService>().AnonymizeExpiredVisitorsAsync();
            }

            // sin plazo configurado no se anonimiza a nadie de este conjunto
            await RunAsync();
            Assert.Equal("Antiguo", (await (await scene.Admin.GetAsync($"/api/visits/{old.Id}")).ReadDataAsync<VisitDto>()).Person.FirstName);

            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitorRetentionDays, "30")).EnsureSuccessStatusCode();
            await api.WithDbAsync(db => db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"AccessEvents\" DISABLE TRIGGER trg_access_events_immutable; " +
                "UPDATE \"AccessEvents\" SET \"OccurredAt\" = now() - interval '45 days' WHERE \"PersonId\" = {0}; " +
                "ALTER TABLE \"AccessEvents\" ENABLE TRIGGER trg_access_events_immutable;", old.Person.Id), scene.ComplexId);

            Assert.True(await RunAsync() >= 1);

            var anonymized = (await (await scene.Admin.GetAsync($"/api/visits/{old.Id}")).ReadDataAsync<VisitDto>()).Person;
            Assert.Equal(("Visitante", "anonimizado", "ANON"), (anonymized.FirstName, anonymized.LastName, anonymized.DocumentType));
            Assert.DoesNotContain(old.Person.DocumentNumber, anonymized.DocumentNumber);
            // el visitante reciente y el residente no se tocan; los eventos siguen ahí
            Assert.Equal("Reciente", (await (await scene.Admin.GetAsync($"/api/visits/{recent.Id}")).ReadDataAsync<VisitDto>()).Person.FirstName);
            var events = await api.WithDbAsync(db => db.AccessEvents.CountAsync(e => e.PersonId == old.Person.Id), scene.ComplexId);
            Assert.Equal(2, events);
        }

        // ---------- HU-132: monitoreo ----------

        [Fact]
        public async Task El_endpoint_de_salud_responde_sin_sesion_e_informa_de_la_base()
        {
            var response = await api.Factory.CreateClient().GetAsync("/api/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"ok\"", body);
            Assert.Contains("\"database\":true", body);
            Assert.Contains("\"pendingMigrations\":0", body);
        }
    }
}
