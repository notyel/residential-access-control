using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class AccessTests(ApiFixture api)
    {
        private static DateOnly Today => TestData.Today;

        private sealed record Scene(HttpClient Admin, HttpClient Guard, Guid ComplexId, UnitDto Unit, Guid ResidentPersonId, HttpClient Resident);

        /// <summary>Conjunto propio con una unidad, un arrendatario con cuenta y un guarda.</summary>
        private async Task<Scene> SceneAsync()
        {
            var (admin, complex, _) = await TestData.CreateComplexAsync(api, "Acceso");
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

        private static object Visitor(string? document = null) => new
        {
            firstName = "Víctor", lastName = "Visitante", documentType = "CC", documentNumber = document ?? HttpExtensions.UniqueDocument()
        };

        private static async Task<AuthorizationDto> AuthorizeAsync(HttpClient resident, Guid unitId, object? extra = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["unitId"] = unitId, ["visitorName"] = "Víctor Visitante", ["kind"] = AuthorizationKind.Single, ["startDate"] = Today
            };
            foreach (var property in extra?.GetType().GetProperties() ?? [])
            {
                body[property.Name] = property.GetValue(extra);
            }
            return await (await resident.PostAsJsonAsync("/api/my/authorizations", body)).ReadDataAsync<AuthorizationDto>();
        }

        // ---------- HU-121 / HU-122: autorizar, validar, ingresar y salir con trazabilidad ----------

        [Fact]
        public async Task Un_residente_autoriza_y_porteria_registra_ingreso_y_salida_con_trazabilidad()
        {
            var scene = await SceneAsync();
            var document = HttpExtensions.UniqueDocument();
            var authorization = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new { visitorDocument = document, reason = "domicilio" });
            Assert.Equal("Rosa Residente", authorization.AuthorizedByName);
            Assert.True(authorization.CanEnterNow);

            // portería busca por documento y ve que puede ingresar
            var lookup = await (await scene.Guard.GetAsync($"/api/access/lookup?term={document}")).ReadDataAsync<LookupResultDto>();
            Assert.True(Assert.Single(lookup.Authorizations).CanEnterNow);
            Assert.Contains((await (await scene.Guard.GetAsync("/api/access/expected")).ReadDataAsync<List<AuthorizationDto>>()), a => a.Id == authorization.Id);

            var visit = await (await scene.Guard.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = scene.Unit.Id, authorizationId = authorization.Id, reason = "domicilio", vehiclePlate = "abc-123",
                newPerson = Visitor(document)
            })).ReadDataAsync<VisitDto>();

            Assert.Equal(VerificationResult.PriorAuthorization, visit.Verification);
            Assert.Equal("Rosa Residente", visit.AuthorizedByName);
            Assert.Equal("Gil Guarda", visit.EntryOperator);
            Assert.Equal("ABC123", visit.VehiclePlate);
            Assert.Null(visit.ExitAt);

            // la autorización única quedó consumida: no sirve para un segundo ingreso y ya no se espera
            var again = await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, authorizationId = authorization.Id, personId = visit.Person.Id });
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            Assert.Equal("La autorización era para un solo ingreso y ya se usó.", (await again.ReadErrorAsync()).Message);
            Assert.DoesNotContain((await (await scene.Guard.GetAsync("/api/access/expected")).ReadDataAsync<List<AuthorizationDto>>()), a => a.Id == authorization.Id);

            var inside = await (await scene.Guard.GetAsync("/api/visits?status=Inside")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visit.Id, Assert.Single(inside.Items!).Id);

            var left = await (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).ReadDataAsync<VisitDto>();
            Assert.NotNull(left.ExitAt);
            Assert.Equal("Gil Guarda", left.ExitOperator);
            Assert.NotEqual(left.EntryEventId, left.ExitEventId);

            // segunda salida: 409 y la hora original no cambia
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).StatusCode);
            var reloaded = await (await scene.Guard.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>();
            Assert.Equal(left.ExitAt, reloaded.ExitAt);
            Assert.Equal(left.ExitEventId, reloaded.ExitEventId);
            Assert.Empty((await (await scene.Guard.GetAsync("/api/visits?status=Inside")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!);

            // el residente ve la visita de su unidad en su historial
            var mine = await (await scene.Resident.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visit.Id, Assert.Single(mine.Items!).Id);
        }

        [Fact]
        public async Task Una_autorizacion_revocada_o_vencida_no_permite_el_ingreso()
        {
            var scene = await SceneAsync();
            var revoked = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new { kind = AuthorizationKind.Period, endDate = Today.AddDays(3) });
            (await scene.Resident.PostAsJsonAsync($"/api/my/authorizations/{revoked.Id}/revoke", new { reason = "Ya no viene" })).EnsureSuccessStatusCode();

            var expired = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new { kind = AuthorizationKind.Period, endDate = Today.AddDays(1), visitorName = "Vencida" });
            // se deja vencida directamente en la base: el API no permite crear una que ya terminó
            await api.WithDbAsync(db => db.VisitAuthorizations.Where(a => a.Id == expired.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.StartDate, Today.AddDays(-5)).SetProperty(a => a.EndDate, Today.AddDays(-1))), scene.ComplexId);

            async Task<HttpResponseMessage> CheckInAsync(Guid authorizationId) =>
                await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, authorizationId, newPerson = Visitor() });

            var withRevoked = await CheckInAsync(revoked.Id);
            Assert.Equal(HttpStatusCode.Conflict, withRevoked.StatusCode);
            Assert.Equal("La autorización fue revocada.", (await withRevoked.ReadErrorAsync()).Message);

            var withExpired = await CheckInAsync(expired.Id);
            Assert.Equal(HttpStatusCode.Conflict, withExpired.StatusCode);
            Assert.Equal("La autorización está vencida.", (await withExpired.ReadErrorAsync()).Message);

            Assert.Empty((await (await scene.Guard.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!);

            var list = await (await scene.Resident.GetAsync("/api/my/authorizations?state=Revoked")).ReadDataAsync<PaginatedResultDto<AuthorizationDto>>();
            Assert.Equal("Ya no viene", Assert.Single(list.Items!).RevokeReason);
        }

        [Fact]
        public async Task Sin_autorizacion_previa_la_politica_del_conjunto_decide()
        {
            var scene = await SceneAsync();
            var stranger = await TestData.CreatePersonAsync(scene.Admin, "No", "Vinculado");

            async Task<HttpResponseMessage> CheckInAsync(object? confirmation = null)
            {
                var body = new Dictionary<string, object?> { ["unitId"] = scene.Unit.Id, ["newPerson"] = Visitor() };
                foreach (var property in confirmation?.GetType().GetProperties() ?? [])
                {
                    body[property.Name] = property.GetValue(confirmation);
                }
                return await scene.Guard.PostAsJsonAsync("/api/access/check-in", body);
            }

            // por defecto: con confirmación de un ocupante de la unidad
            Assert.Equal(HttpStatusCode.BadRequest, (await CheckInAsync()).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await CheckInAsync(new { confirmedByPersonId = stranger.Id, confirmationChannel = "citófono" })).StatusCode);
            var confirmed = await (await CheckInAsync(new { confirmedByPersonId = scene.ResidentPersonId, confirmationChannel = "citófono" })).ReadDataAsync<VisitDto>();
            Assert.Equal(VerificationResult.ResidentConfirmation, confirmed.Verification);

            // no permitidas
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.NotAllowed)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Conflict, (await CheckInAsync(new { confirmedByPersonId = scene.ResidentPersonId, confirmationChannel = "citófono" })).StatusCode);

            // a criterio de portería
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var approved = await (await CheckInAsync()).ReadDataAsync<VisitDto>();
            Assert.Equal(VerificationResult.GuardApproval, approved.Verification);
        }

        [Fact]
        public async Task El_acceso_excepcional_exige_permiso_y_motivo_y_deja_al_responsable()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.NotAllowed)).EnsureSuccessStatusCode();
            object Body(string? reason) => new { unitId = scene.Unit.Id, newPerson = Visitor(), isException = true, exceptionReason = reason };

            // portería no tiene el permiso
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync("/api/access/check-in", Body("Emergencia médica en la unidad"))).StatusCode);
            // sin motivo suficiente
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync("/api/access/check-in", Body("urgente"))).StatusCode);

            var visit = await (await scene.Admin.PostAsJsonAsync("/api/access/check-in", Body("Emergencia médica en la unidad"))).ReadDataAsync<VisitDto>();
            Assert.Equal(VerificationResult.Exception, visit.Verification);
            Assert.Equal("Emergencia médica en la unidad", visit.ExceptionReason);
            Assert.Equal("Ana Admin", visit.EntryOperator);

            var exceptions = await (await scene.Admin.GetAsync("/api/visits?verification=Exception")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visit.Id, Assert.Single(exceptions.Items!).Id);
        }

        [Fact]
        public async Task El_rechazo_queda_registrado_sin_crear_una_visita_y_el_documento_sigue_la_politica()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();

            var rejection = await (await scene.Guard.PostAsJsonAsync("/api/access/reject", new
            {
                unitId = scene.Unit.Id, personName = "Desconocido Insistente", reasonCode = "sin-autorizacion", notes = "Dijo venir de parte de alguien"
            })).ReadDataAsync<AccessEventDto>();
            Assert.Equal(AccessEventType.Rejection, rejection.Type);
            Assert.Equal("Sin autorización", rejection.Reason);
            Assert.Equal("Gil Guarda", rejection.OperatorName);
            Assert.Empty((await (await scene.Guard.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!);

            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Guard.PostAsJsonAsync("/api/access/reject", new { personName = "X", reasonCode = "inventado" })).StatusCode);

            // documento obligatorio por defecto
            object NoDocument() => new { unitId = scene.Unit.Id, newPerson = new { firstName = "Sin", lastName = "Documento", documentType = "", documentNumber = "" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Guard.PostAsJsonAsync("/api/access/check-in", NoDocument())).StatusCode);

            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitorDocumentRequired, "false")).EnsureSuccessStatusCode();
            var visit = await (await scene.Guard.PostAsJsonAsync("/api/access/check-in", NoDocument())).ReadDataAsync<VisitDto>();
            Assert.Equal("NA", visit.Person.DocumentType);
        }

        // ---------- HU-120: eventos separados e inmutables ----------

        [Fact]
        public async Task Los_eventos_no_se_modifican_y_un_error_se_corrige_anulando()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var visit = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id);

            // ni siquiera un UPDATE directo en la base pasa
            var update = await Assert.ThrowsAnyAsync<Exception>(() => api.WithDbAsync(db =>
                db.Database.ExecuteSqlRawAsync("UPDATE \"AccessEvents\" SET \"Notes\" = 'alterado'"), scene.ComplexId));
            Assert.Contains("no se modifican ni se borran", update.ToString());
            await Assert.ThrowsAnyAsync<Exception>(() => api.WithDbAsync(db =>
                db.Database.ExecuteSqlRawAsync("DELETE FROM \"AccessEvents\""), scene.ComplexId));

            // anular exige permiso y motivo
            Assert.Equal(HttpStatusCode.Forbidden, (await scene.Guard.PostAsJsonAsync($"/api/access/events/{visit.EntryEventId}/void", new { reason = "Me equivoqué de unidad" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Admin.PostAsJsonAsync($"/api/access/events/{visit.EntryEventId}/void", new { reason = "" })).StatusCode);

            var voiding = await (await scene.Admin.PostAsJsonAsync($"/api/access/events/{visit.EntryEventId}/void", new { reason = "Me equivoqué de unidad" })).ReadDataAsync<AccessEventDto>();
            Assert.Equal(AccessEventType.Void, voiding.Type);
            Assert.Equal(visit.EntryEventId, voiding.VoidsEventId);

            // el original sigue ahí; la visita queda marcada y deja de contar como presente
            var events = await api.WithDbAsync(db => db.AccessEvents.Where(e => e.VisitId == visit.Id).OrderBy(e => e.OccurredAt).ToListAsync(), scene.ComplexId);
            Assert.Equal(new[] { AccessEventType.Entry, AccessEventType.Void }, events.Select(e => e.Type));
            Assert.True((await (await scene.Admin.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>()).IsVoided);
            Assert.Empty((await (await scene.Admin.GetAsync("/api/visits?status=Inside")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!);

            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PostAsJsonAsync($"/api/access/events/{visit.EntryEventId}/void", new { reason = "Otra vez lo mismo" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await scene.Admin.PostAsJsonAsync($"/api/access/events/{voiding.Id}/void", new { reason = "Anular la anulación" })).StatusCode);
        }

        [Fact]
        public async Task Anular_una_salida_devuelve_la_visita_a_presente_y_el_cierre_administrativo_exige_motivo()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var visit = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id);

            Assert.Equal(HttpStatusCode.BadRequest, (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { administrative = true })).StatusCode);
            var closed = await (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { administrative = true, reason = "Nadie registró su salida ayer" })).ReadDataAsync<VisitDto>();
            Assert.True(closed.ExitIsAdministrative);

            (await scene.Admin.PostAsJsonAsync($"/api/access/events/{closed.ExitEventId}/void", new { reason = "Sí seguía dentro" })).EnsureSuccessStatusCode();
            var reopened = await (await scene.Admin.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>();
            Assert.Null(reopened.ExitAt);
            Assert.False(reopened.IsVoided);
            Assert.Single((await (await scene.Admin.GetAsync("/api/visits?status=Inside")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!);
        }

        [Fact]
        public async Task Porteria_registra_la_entrada_de_un_residente_solo_si_tiene_vinculo_vigente()
        {
            var scene = await SceneAsync();
            var stranger = await TestData.CreatePersonAsync(scene.Admin);

            var entry = await (await scene.Guard.PostAsJsonAsync("/api/access/residents", new
            {
                personId = scene.ResidentPersonId, unitId = scene.Unit.Id, type = AccessEventType.Entry
            })).ReadDataAsync<AccessEventDto>();
            Assert.Equal(PersonKind.Resident, entry.PersonKind);
            Assert.Equal("Rosa Residente", entry.PersonName);

            var notLinked = await scene.Guard.PostAsJsonAsync("/api/access/residents", new { personId = stranger.Id, unitId = scene.Unit.Id, type = AccessEventType.Entry });
            Assert.Equal(HttpStatusCode.Conflict, notLinked.StatusCode);

            var lookup = await (await scene.Guard.GetAsync("/api/access/lookup?term=Rosa")).ReadDataAsync<LookupResultDto>();
            Assert.Contains(lookup.Residents, r => r.PersonId == scene.ResidentPersonId && r.UnitId == scene.Unit.Id);
        }

        // ---------- HU-121: reglas de las autorizaciones ----------

        [Fact]
        public async Task Las_reglas_de_quien_autoriza_y_por_cuanto_tiempo_se_aplican()
        {
            var scene = await SceneAsync();
            var other = await TestData.CreateOwnerWithUnitAsync(api, scene.Admin);

            async Task<HttpStatusCode> CreateAsync(HttpClient client, string path, object body) => (await client.PostAsJsonAsync(path, body)).StatusCode;
            object Body(Guid unitId, object? extra = null)
            {
                var body = new Dictionary<string, object?> { ["unitId"] = unitId, ["visitorName"] = "Alguien", ["kind"] = AuthorizationKind.Period, ["startDate"] = Today, ["endDate"] = Today.AddDays(2) };
                foreach (var property in extra?.GetType().GetProperties() ?? [])
                {
                    body[property.Name] = property.GetValue(extra);
                }
                return body;
            }

            // unidad ajena: como si no existiera
            Assert.Equal(HttpStatusCode.NotFound, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(other.UnitId)));
            // más larga que la política (30 días por defecto)
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(scene.Unit.Id, new { endDate = Today.AddDays(40) })));
            // recurrente sin días, franja incompleta, sin visitante
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(scene.Unit.Id, new { kind = AuthorizationKind.Recurring })));
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(scene.Unit.Id, new { timeFrom = "08:00" })));
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(scene.Unit.Id, new { visitorName = "" })));

            // la administración debe indicar quién de la unidad autoriza, y debe estar vinculado
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Admin, "/api/authorizations", Body(scene.Unit.Id)));
            Assert.Equal(HttpStatusCode.BadRequest, await CreateAsync(scene.Admin, "/api/authorizations", Body(scene.Unit.Id, new { authorizedByPersonId = other.PersonId })));
            Assert.Equal(HttpStatusCode.OK, await CreateAsync(scene.Admin, "/api/authorizations", Body(scene.Unit.Id, new { authorizedByPersonId = scene.ResidentPersonId })));

            // si la política no deja autorizar a los residentes, el propietario sí puede
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.ResidentCanAuthorize, "false")).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, await CreateAsync(scene.Resident, "/api/my/authorizations", Body(scene.Unit.Id)));
            Assert.Equal(HttpStatusCode.OK, await CreateAsync(other.Client, "/api/my/authorizations", Body(other.UnitId)));

            // portería consulta pero no crea
            Assert.Equal(HttpStatusCode.OK, (await scene.Guard.GetAsync("/api/authorizations")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, await CreateAsync(scene.Guard, "/api/authorizations", Body(scene.Unit.Id, new { authorizedByPersonId = scene.ResidentPersonId })));
        }

        [Fact]
        public async Task Al_terminar_la_ocupacion_se_revocan_las_autorizaciones_de_quien_se_va()
        {
            var scene = await SceneAsync();
            var authorization = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new { kind = AuthorizationKind.Period, endDate = Today.AddDays(10) });
            var occupancy = Assert.Single((await (await scene.Admin.GetAsync($"/api/units/{scene.Unit.Id}/summary")).ReadDataAsync<UnitSummaryDto>()).Occupants);

            (await scene.Admin.PostAsJsonAsync($"/api/occupancies/{occupancy.Id}/end", new { endDate = Today.AddDays(-1) })).EnsureSuccessStatusCode();

            var revoked = await (await scene.Admin.GetAsync("/api/authorizations?state=Revoked")).ReadDataAsync<PaginatedResultDto<AuthorizationDto>>();
            var item = Assert.Single(revoked.Items!);
            Assert.Equal(authorization.Id, item.Id);
            Assert.Equal("Vinculación finalizada", item.RevokeReason);

            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var checkIn = await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, authorizationId = authorization.Id, newPerson = Visitor() });
            Assert.Equal(HttpStatusCode.Conflict, checkIn.StatusCode);
        }

        [Fact]
        public async Task Una_autorizacion_recurrente_se_espera_los_dias_que_aplica_y_admite_varios_ingresos()
        {
            var scene = await SceneAsync();
            var today = (int)Today.DayOfWeek;
            var recurring = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new
            {
                kind = AuthorizationKind.Recurring, endDate = Today.AddDays(20), daysOfWeek = new[] { today }, visitorName = "Aseo Semanal"
            });
            var otherDay = await AuthorizeAsync(scene.Resident, scene.Unit.Id, new
            {
                kind = AuthorizationKind.Recurring, endDate = Today.AddDays(20), daysOfWeek = new[] { (today + 3) % 7 }, visitorName = "Otro Día"
            });
            Assert.False(otherDay.CanEnterNow);
            Assert.Equal("La autorización no aplica para hoy.", otherDay.CannotEnterReason);

            var expected = await (await scene.Guard.GetAsync("/api/access/expected")).ReadDataAsync<List<AuthorizationDto>>();
            Assert.Contains(expected, a => a.Id == recurring.Id);
            Assert.DoesNotContain(expected, a => a.Id == otherDay.Id);

            var first = await (await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, authorizationId = recurring.Id, newPerson = Visitor() })).ReadDataAsync<VisitDto>();
            (await scene.Guard.PostAsJsonAsync($"/api/access/visits/{first.Id}/check-out", new { })).EnsureSuccessStatusCode();

            // sigue vigente para otro ingreso
            var second = await scene.Guard.PostAsJsonAsync("/api/access/check-in", new { unitId = scene.Unit.Id, authorizationId = recurring.Id, personId = first.Person.Id });
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }

        // ---------- HU-123: historial ----------

        [Fact]
        public async Task El_historial_se_filtra_por_unidad_persona_placa_y_verificacion()
        {
            var scene = await SceneAsync();
            (await TestData.SetPolicyAsync(scene.Admin, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            var otherUnit = await TestData.CreateUnitAsync(scene.Admin);
            var withPlate = await TestData.CheckInAsync(scene.Guard, scene.Unit.Id, "xyz 987", "Zulma");
            var elsewhere = await TestData.CheckInAsync(scene.Guard, otherUnit.Id, firstName: "Yolanda");

            async Task<List<VisitDto>> ListAsync(string query) =>
                (await (await scene.Guard.GetAsync($"/api/visits?{query}")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!.ToList();

            Assert.Equal(withPlate.Id, Assert.Single(await ListAsync($"unitId={scene.Unit.Id}")).Id);
            Assert.Equal(withPlate.Id, Assert.Single(await ListAsync("plate=XYZ-987")).Id);
            Assert.Equal(elsewhere.Id, Assert.Single(await ListAsync("search=yolan")).Id);
            Assert.Equal(withPlate.Id, Assert.Single(await ListAsync($"search={withPlate.Person.DocumentNumber}")).Id);
            Assert.Equal(2, (await ListAsync("verification=GuardApproval")).Count);
            Assert.Empty(await ListAsync("verification=PriorAuthorization"));

            // el residente solo ve su unidad
            var mine = (await (await scene.Resident.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!;
            Assert.Equal(withPlate.Id, Assert.Single(mine).Id);
            Assert.Equal(HttpStatusCode.NotFound, (await scene.Resident.GetAsync($"/api/visits/{elsewhere.Id}")).StatusCode);
        }
    }
}
