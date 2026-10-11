using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Role;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class UnitTests(ApiFixture api)
    {
        private static DateOnly Today => TestData.Today;

        private static async Task<UnitSummaryDto> SummaryAsync(HttpClient client, Guid unitId) =>
            await (await client.GetAsync($"/api/units/{unitId}/summary")).ReadDataAsync<UnitSummaryDto>();

        private static Task<HttpResponseMessage> SetPolicyAsync(HttpClient admin, string key, string value) =>
            admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string> { [key] = value });

        // ---------- HU-115 Estructura y unidades ----------

        [Fact]
        public async Task El_codigo_de_la_unidad_es_unico_en_el_conjunto_y_la_etiqueta_incluye_su_estructura()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var tower = await (await admin.PostAsJsonAsync("/api/structures", new { type = "torre", name = "Torre A" })).ReadDataAsync<StructureDto>();
            var floor = await (await admin.PostAsJsonAsync("/api/structures", new { type = "piso", name = "Piso 1", parentId = tower.Id })).ReadDataAsync<StructureDto>();
            Assert.Equal("Torre A · Piso 1", floor.Path);

            var unit = await (await admin.PostAsJsonAsync("/api/units", new { code = "101", type = "apartamento", structureId = floor.Id })).ReadDataAsync<UnitDto>();
            Assert.Equal("Torre A · Piso 1 · 101", unit.Label);
            Assert.Equal(UnitOccupancyState.Vacant, unit.OccupancyState);

            // el mismo código no se repite aunque sea en otra estructura ni con otras mayúsculas
            var duplicated = await admin.PostAsJsonAsync("/api/units", new { code = "101", type = "local" });
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);

            var badType = await admin.PostAsJsonAsync("/api/units", new { code = "999", type = "castillo" });
            Assert.Equal(HttpStatusCode.BadRequest, badType.StatusCode);

            // renombrar la estructura actualiza la etiqueta de sus unidades
            (await admin.PutAsJsonAsync($"/api/structures/{tower.Id}", new { type = "torre", name = "Torre Norte" })).EnsureSuccessStatusCode();
            Assert.Equal("Torre Norte · Piso 1 · 101", (await (await admin.GetAsync($"/api/units/{unit.Id}")).ReadDataAsync<UnitDto>()).Label);

            // una estructura con unidades no se elimina, ni puede contenerse a sí misma
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/structures/{floor.Id}")).StatusCode);
            var cycle = await admin.PutAsJsonAsync($"/api/structures/{tower.Id}", new { type = "torre", name = "Torre Norte", parentId = floor.Id });
            Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
        }

        [Fact]
        public async Task La_creacion_en_lote_muestra_vista_previa_y_omite_los_codigos_que_ya_existen()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            await TestData.CreateUnitAsync(admin, "A101");
            var request = new { type = "apartamento", prefix = "A", floorFrom = 1, floorTo = 3, unitsPerFloor = 4 };

            var preview = await (await admin.PostAsJsonAsync("/api/units/bulk", new { request.type, request.prefix, request.floorFrom, request.floorTo, request.unitsPerFloor, preview = true }))
                .ReadDataAsync<BulkUnitsResultDto>();
            Assert.Equal(11, preview.Created.Count);
            Assert.Equal("A101", Assert.Single(preview.Skipped));
            Assert.Contains("A304", preview.Created);

            // la vista previa no guardó nada
            var before = await (await admin.GetAsync("/api/units?pageSize=100")).ReadDataAsync<PaginatedResultDto<UnitDto>>();
            Assert.Equal(1, before.TotalCount);

            await (await admin.PostAsJsonAsync("/api/units/bulk", request)).ReadDataAsync<BulkUnitsResultDto>();
            var after = await (await admin.GetAsync("/api/units?pageSize=100")).ReadDataAsync<PaginatedResultDto<UnitDto>>();
            Assert.Equal(12, after.TotalCount);
        }

        [Fact]
        public async Task Una_unidad_inactiva_conserva_su_historial_y_no_admite_propietarios_ocupantes_ni_visitas()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin);
            await TestData.AddOwnerAsync(admin, unit.Id, person.Id);

            (await admin.PostAsync($"/api/units/{unit.Id}/deactivate", null)).EnsureSuccessStatusCode();

            var other = await TestData.CreatePersonAsync(admin);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/units/{unit.Id}/owners", new { personId = other.Id, startDate = Today })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/occupancies", new { unitId = unit.Id, personId = other.Id, relation = OccupancyRelation.Tenant, startDate = Today })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/access/check-in", new { unitId = unit.Id, personId = other.Id })).StatusCode);

            Assert.Single((await SummaryAsync(admin, unit.Id)).Owners);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.DeleteAsync($"/api/units/{unit.Id}")).StatusCode);
        }

        [Fact]
        public async Task Los_catalogos_tienen_valores_iniciales_y_cada_conjunto_ajusta_los_suyos()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var (other, _, _) = await TestData.CreateComplexAsync(api);

            var initial = await (await admin.GetAsync("/api/catalogs/unit-type")).ReadDataAsync<List<CatalogItemDto>>();
            Assert.Contains(initial, i => i.Code == "apartamento");

            var updated = await (await admin.PutAsJsonAsync("/api/catalogs/unit-type", new[]
            {
                new { code = "casa", label = "Casa", isActive = true },
                new { code = "cabaña", label = "Cabaña", isActive = true }
            })).ReadDataAsync<List<CatalogItemDto>>();
            Assert.Equal(new[] { "casa", "cabaña" }, updated.Select(i => i.Code));

            // "apartamento" ya no es válido aquí, pero sí en el otro conjunto
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/units", new { code = "1", type = "apartamento" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/units", new { code = "1", type = "cabaña" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await other.PostAsJsonAsync("/api/units", new { code = "1", type = "apartamento" })).StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/catalogs/no-existe")).StatusCode);
        }

        // ---------- HU-116 Personas y propietarios ----------

        [Fact]
        public async Task Una_unidad_admite_varios_propietarios_y_una_persona_puede_serlo_de_varias_unidades()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unitA = await TestData.CreateUnitAsync(admin);
            var unitB = await TestData.CreateUnitAsync(admin);
            var first = await TestData.CreatePersonAsync(admin, "Uno", "Dueño");
            var second = await TestData.CreatePersonAsync(admin, "Dos", "Dueña");

            await TestData.AddOwnerAsync(admin, unitA.Id, first.Id);
            await TestData.AddOwnerAsync(admin, unitA.Id, second.Id);
            await TestData.AddOwnerAsync(admin, unitB.Id, first.Id);

            Assert.Equal(2, (await SummaryAsync(admin, unitA.Id)).Owners.Count);
            var detail = await (await admin.GetAsync($"/api/persons/{first.Id}")).ReadDataAsync<PersonDetailDto>();
            Assert.Equal(2, detail.Ownerships.Count(o => o.IsCurrent));

            // la misma persona no puede tener dos titularidades vigentes sobre la misma unidad
            var again = await admin.PostAsJsonAsync($"/api/units/{unitA.Id}/owners", new { personId = first.Id, startDate = Today });
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        }

        [Fact]
        public async Task Con_la_politica_de_un_solo_propietario_el_segundo_se_rechaza()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            (await SetPolicyAsync(admin, ComplexPolicies.MultipleOwners, "false")).EnsureSuccessStatusCode();
            var unit = await TestData.CreateUnitAsync(admin);
            await TestData.AddOwnerAsync(admin, unit.Id, (await TestData.CreatePersonAsync(admin)).Id);

            var second = await admin.PostAsJsonAsync($"/api/units/{unit.Id}/owners", new { personId = (await TestData.CreatePersonAsync(admin)).Id, startDate = Today });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task El_cambio_de_propietario_cierra_la_titularidad_anterior_y_conserva_el_historial()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var seller1 = await TestData.CreatePersonAsync(admin, "Vende", "Uno");
            var seller2 = await TestData.CreatePersonAsync(admin, "Vende", "Dos");
            var buyer = await TestData.CreatePersonAsync(admin, "Compra", "Nueva");
            await TestData.AddOwnerAsync(admin, unit.Id, seller1.Id, Today.AddYears(-3));
            await TestData.AddOwnerAsync(admin, unit.Id, seller2.Id, Today.AddYears(-3));

            var transfer = await admin.PostAsJsonAsync($"/api/units/{unit.Id}/owners/transfer", new { personIds = new[] { buyer.Id }, date = Today });
            await transfer.ReadDataAsync<List<OwnershipDto>>();

            var summary = await SummaryAsync(admin, unit.Id);
            Assert.Equal("Compra Nueva", Assert.Single(summary.Owners).PersonName);
            Assert.Equal(2, summary.PastOwners!.Count);
            Assert.All(summary.PastOwners, o => Assert.Equal(Today.AddDays(-1), o.EndDate));
            Assert.All(summary.PastOwners, o => Assert.Equal(Today.AddYears(-3), o.StartDate));

            // la ficha de quien vendió sigue mostrando que fue propietaria
            var seller = await (await admin.GetAsync($"/api/persons/{seller1.Id}")).ReadDataAsync<PersonDetailDto>();
            Assert.False(Assert.Single(seller.Ownerships).IsCurrent);
        }

        [Fact]
        public async Task El_directorio_busca_personas_y_distingue_a_quienes_tienen_vinculo_vigente()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var owner = await TestData.CreatePersonAsync(admin, "Zoraida", "Propietaria");
            var visitor = await TestData.CreatePersonAsync(admin, "Zacarías", "Visitante");
            await TestData.AddOwnerAsync(admin, unit.Id, owner.Id);

            var all = await (await admin.GetAsync("/api/persons/search?search=z")).ReadDataAsync<PaginatedResultDto<PersonListItemDto>>();
            Assert.Equal(2, all.TotalCount);

            var linked = await (await admin.GetAsync("/api/persons/search?onlyLinked=true")).ReadDataAsync<PaginatedResultDto<PersonListItemDto>>();
            var item = Assert.Single(linked.Items!);
            Assert.Equal(owner.Id, item.Id);
            Assert.Equal(unit.Label, Assert.Single(item.OwnedUnits));
            Assert.False(item.HasUser);

            // documento duplicado al crear y al corregir
            var duplicated = await admin.PostAsJsonAsync("/api/persons", new { firstName = "X", lastName = "Y", documentType = "CC", documentNumber = owner.DocumentNumber });
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);
            var update = await admin.PutAsJsonAsync($"/api/persons/{visitor.Id}", new { firstName = "Z", lastName = "V", documentType = "CC", documentNumber = owner.DocumentNumber });
            Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
            var badType = await admin.PostAsJsonAsync("/api/persons", new { firstName = "X", lastName = "Y", documentType = "ZZ", documentNumber = "1" });
            Assert.Equal(HttpStatusCode.BadRequest, badType.StatusCode);
        }

        [Fact]
        public async Task Dar_acceso_crea_una_cuenta_invitada_enlazada_a_la_persona_y_solo_una()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var person = await TestData.CreatePersonAsync(admin, "Carla", "Cuenta");

            var (_, client) = await TestData.GrantAccessAsync(api, admin, person.Id, SystemRoles.Owner);
            var me = await (await client.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();
            Assert.Equal("Carla", me.FirstName);
            Assert.Equal(SystemRoles.Owner, Assert.Single(me.Roles).SystemKey);

            var detail = await (await admin.GetAsync($"/api/persons/{person.Id}")).ReadDataAsync<PersonDetailDto>();
            Assert.Equal(me.Id, detail.User!.Id);

            var again = await admin.PostAsJsonAsync($"/api/persons/{person.Id}/grant-access", new { email = "otra@test.com", roleKey = SystemRoles.Owner });
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            var asAdmin = await admin.PostAsJsonAsync($"/api/persons/{(await TestData.CreatePersonAsync(admin)).Id}/grant-access", new { email = "x@test.com", roleKey = SystemRoles.Admin });
            Assert.Equal(HttpStatusCode.BadRequest, asAdmin.StatusCode);
        }

        // ---------- HU-117 Ocupantes y arrendamientos ----------

        [Fact]
        public async Task El_arrendatario_no_se_confunde_con_el_propietario_y_el_estado_de_la_unidad_se_calcula()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var owner = await TestData.CreatePersonAsync(admin, "Omar", "Dueño");
            var tenant = await TestData.CreatePersonAsync(admin, "Tania", "Inquilina");
            await TestData.AddOwnerAsync(admin, unit.Id, owner.Id);
            Assert.Equal(UnitOccupancyState.Vacant, (await SummaryAsync(admin, unit.Id)).Unit.OccupancyState);

            await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant, Today.AddMonths(-1), Today.AddMonths(5));

            var summary = await SummaryAsync(admin, unit.Id);
            Assert.Equal(UnitOccupancyState.Rented, summary.Unit.OccupancyState);
            Assert.Equal("Omar Dueño", Assert.Single(summary.Owners).PersonName);
            var occupant = Assert.Single(summary.Occupants);
            Assert.Equal("Tania Inquilina", occupant.PersonName);
            Assert.Equal(OccupancyRelation.Tenant, occupant.Relation);

            var rented = await (await admin.GetAsync("/api/units?state=Rented")).ReadDataAsync<PaginatedResultDto<UnitDto>>();
            Assert.Equal(unit.Id, Assert.Single(rented.Items!).Id);
        }

        [Fact]
        public async Task Las_reglas_de_la_ocupacion_se_validan()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin);

            async Task<HttpStatusCode> AddAsync(object body) => (await admin.PostAsJsonAsync("/api/occupancies", body)).StatusCode;

            // propietario residente exige titularidad vigente
            Assert.Equal(HttpStatusCode.BadRequest, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.OwnerResident, startDate = Today }));
            // arrendamiento a término fijo exige fecha de fin
            Assert.Equal(HttpStatusCode.BadRequest, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.Tenant, startDate = Today, fixedTerm = true }));
            // fin anterior al inicio
            Assert.Equal(HttpStatusCode.BadRequest, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.Tenant, startDate = Today, endDate = Today.AddDays(-1) }));

            Assert.Equal(HttpStatusCode.OK, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.Tenant, startDate = Today.AddMonths(-2), endDate = Today.AddMonths(2) }));
            // dos ocupaciones de la misma persona que se cruzan en la misma unidad
            Assert.Equal(HttpStatusCode.Conflict, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.Family, startDate = Today }));
            // después del periodo sí
            Assert.Equal(HttpStatusCode.OK, await AddAsync(new { unitId = unit.Id, personId = person.Id, relation = OccupancyRelation.Tenant, startDate = Today.AddMonths(2).AddDays(1) }));
        }

        [Fact]
        public async Task Finalizar_el_arrendamiento_revoca_el_acceso_y_conserva_el_historial()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var tenant = await TestData.CreatePersonAsync(admin, "Tania", "Inquilina");
            var relative = await TestData.CreatePersonAsync(admin, "Hijo", "Inquilina");
            var lease = await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant);
            await TestData.AddOccupantAsync(admin, unit.Id, relative.Id, OccupancyRelation.Family);
            var (email, client) = await TestData.GrantAccessAsync(api, admin, tenant.Id, SystemRoles.Resident);
            Assert.Single(await (await client.GetAsync("/api/my/units")).ReadDataAsync<List<MyUnitDto>>());

            var ended = await (await admin.PostAsJsonAsync($"/api/occupancies/{lease.Id}/end", new
            {
                endDate = Today.AddDays(-1), reason = "Terminó el contrato", includeDependents = true
            })).ReadDataAsync<OccupancyDto>();
            Assert.Equal(OccupancyStatus.Ended, ended.Status);

            var summary = await SummaryAsync(admin, unit.Id);
            Assert.Equal(UnitOccupancyState.Vacant, summary.Unit.OccupancyState);
            Assert.Empty(summary.Occupants);
            // nada se borró: ambas ocupaciones siguen en el historial con sus fechas
            Assert.Equal(2, summary.PastOccupants!.Count);
            Assert.Contains(summary.PastOccupants, o => o.PersonName == "Tania Inquilina" && o.StatusReason == "Terminó el contrato");

            // sin otro vínculo vigente pierde el rol de residente: conserva la cuenta, no el acceso
            var me = await (await client.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();
            Assert.Empty(me.Roles);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/my/units")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = TestData.Password })).StatusCode);

            // no se puede finalizar dos veces
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/occupancies/{lease.Id}/end", new { endDate = Today })).StatusCode);
        }

        [Fact]
        public async Task Una_ocupacion_vencida_deja_de_contar_y_el_proceso_diario_la_cierra()
        {
            var (admin, complex, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var tenant = await TestData.CreatePersonAsync(admin);
            var lease = await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant, Today.AddMonths(-6), Today.AddDays(-2));

            // sin que nadie la toque ya no es vigente
            var summary = await SummaryAsync(admin, unit.Id);
            Assert.Empty(summary.Occupants);
            Assert.Equal(UnitOccupancyState.Vacant, summary.Unit.OccupancyState);
            Assert.Equal(OccupancyStatus.Active, Assert.Single(summary.PastOccupants!).Status);

            using (var scope = api.Factory.Services.CreateScope())
            {
                var closed = await scope.ServiceProvider.GetRequiredService<IOccupancyService>().CloseExpiredAsync();
                Assert.True(closed >= 1);
            }

            var status = await api.WithDbAsync(db => db.Occupancies.Where(o => o.Id == lease.Id).Select(o => o.Status).FirstAsync(), complex.Id);
            Assert.Equal(OccupancyStatus.Ended, status);
        }

        // ---------- HU-118 Ficha de unidad ----------

        [Fact]
        public async Task Porteria_sin_permiso_de_personas_ve_solo_lo_vigente_sin_historial()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var unit = await TestData.CreateUnitAsync(admin);
            var past = await TestData.CreatePersonAsync(admin);
            var current = await TestData.CreatePersonAsync(admin, "Actual", "Ocupante");
            var old = await TestData.AddOccupantAsync(admin, unit.Id, past.Id, OccupancyRelation.Tenant, Today.AddYears(-2));
            (await admin.PostAsJsonAsync($"/api/occupancies/{old.Id}/end", new { endDate = Today.AddYears(-1) })).EnsureSuccessStatusCode();
            await TestData.AddOccupantAsync(admin, unit.Id, current.Id, OccupancyRelation.Tenant);

            var role = await (await admin.PostAsJsonAsync("/api/roles", new { name = "Portería básica", permissions = new[] { Permissions.UnitsRead } })).ReadDataAsync<RoleDto>();
            var email = $"basico-{Guid.NewGuid():N}@test.com";
            await (await admin.PostAsJsonAsync("/api/users", new { email, firstName = "P", lastName = "B", roleIds = new[] { role.Id } })).ReadDataAsync<UserDto>();
            var guard = await api.ClientForAsync(email, TestData.Password);

            var limited = await SummaryAsync(guard, unit.Id);
            Assert.Equal("Actual Ocupante", Assert.Single(limited.Occupants).PersonName);
            Assert.Null(limited.PastOccupants);
            Assert.Null(limited.PastOwners);
            Assert.Equal(HttpStatusCode.Forbidden, (await guard.GetAsync($"/api/persons/{current.Id}")).StatusCode);

            Assert.Single((await SummaryAsync(admin, unit.Id)).PastOccupants!);
        }

        // ---------- HU-119 Portal de propietario y residente ----------

        [Fact]
        public async Task El_propietario_solo_ve_y_gestiona_sus_unidades_y_lo_que_registra_espera_aprobacion()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var mine = await TestData.CreateOwnerWithUnitAsync(api, admin);
            var theirs = await TestData.CreateOwnerWithUnitAsync(api, admin);

            var units = await (await mine.Client.GetAsync("/api/my/units")).ReadDataAsync<List<MyUnitDto>>();
            var unit = Assert.Single(units);
            Assert.Equal(mine.UnitId, unit.Id);
            Assert.True(unit.IsOwner);
            Assert.Null(unit.Relation); // propietario no residente

            object Occupant(Guid unitId) => new
            {
                unitId, relation = OccupancyRelation.Tenant, startDate = Today,
                firstName = "Inés", lastName = "Inquilina", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument()
            };

            // la unidad de otro responde como inexistente, tanto por el portal como por el API administrativo
            Assert.Equal(HttpStatusCode.NotFound, (await mine.Client.PostAsJsonAsync("/api/my/occupants", Occupant(theirs.UnitId))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await mine.Client.GetAsync($"/api/units/{theirs.UnitId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await mine.Client.PostAsJsonAsync("/api/occupancies", Occupant(theirs.UnitId))).StatusCode);

            // política por defecto: con aprobación
            var requested = await (await mine.Client.PostAsJsonAsync("/api/my/occupants", Occupant(mine.UnitId))).ReadDataAsync<OccupancyDto>();
            Assert.Equal(OccupancyStatus.Pending, requested.Status);
            Assert.Empty((await SummaryAsync(admin, mine.UnitId)).Occupants);

            var pending = await (await admin.GetAsync("/api/occupancies/pending")).ReadDataAsync<List<OccupancyDto>>();
            Assert.Equal(requested.Id, Assert.Single(pending).Id);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/occupancies/{requested.Id}/decide", new { approve = false })).StatusCode);
            (await admin.PostAsJsonAsync($"/api/occupancies/{requested.Id}/decide", new { approve = true })).EnsureSuccessStatusCode();

            Assert.Equal("Inés Inquilina", Assert.Single((await SummaryAsync(admin, mine.UnitId)).Occupants).PersonName);
            var occupants = await (await mine.Client.GetAsync("/api/my/occupants")).ReadDataAsync<List<OccupancyDto>>();
            Assert.Equal(requested.Id, Assert.Single(occupants).Id);

            // no puede finalizar ocupaciones de la unidad de otro
            var foreign = await TestData.AddOccupantAsync(admin, theirs.UnitId, (await TestData.CreatePersonAsync(admin)).Id, OccupancyRelation.Tenant);
            Assert.Equal(HttpStatusCode.NotFound, (await mine.Client.PostAsJsonAsync($"/api/my/occupants/{foreign.Id}/end", new { endDate = Today })).StatusCode);
        }

        [Fact]
        public async Task La_politica_decide_si_el_propietario_registra_directo_o_no_puede()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var owner = await TestData.CreateOwnerWithUnitAsync(api, admin);
            object Occupant() => new
            {
                unitId = owner.UnitId, relation = OccupancyRelation.Family, startDate = Today,
                firstName = "Fam", lastName = "Iliar", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument()
            };

            (await SetPolicyAsync(admin, ComplexPolicies.OwnerManagesOccupants, ComplexPolicies.Direct)).EnsureSuccessStatusCode();
            var direct = await (await owner.Client.PostAsJsonAsync("/api/my/occupants", Occupant())).ReadDataAsync<OccupancyDto>();
            Assert.Equal(OccupancyStatus.Active, direct.Status);

            (await SetPolicyAsync(admin, ComplexPolicies.OwnerManagesOccupants, ComplexPolicies.No)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.Client.PostAsJsonAsync("/api/my/occupants", Occupant())).StatusCode);
            // consultar sí puede
            Assert.Single(await (await owner.Client.GetAsync("/api/my/occupants")).ReadDataAsync<List<OccupancyDto>>());
        }

        [Fact]
        public async Task Quien_es_propietario_de_una_unidad_y_arrendatario_de_otra_ve_ambas_con_su_vinculo()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api);
            var owner = await TestData.CreateOwnerWithUnitAsync(api, admin);
            var rented = await TestData.CreateUnitAsync(admin);
            var neighbour = await TestData.CreatePersonAsync(admin, "Vecina", "Conviviente");
            await TestData.AddOccupantAsync(admin, rented.Id, owner.PersonId, OccupancyRelation.Tenant, Today.AddMonths(-1), Today.AddMonths(3));
            await TestData.AddOccupantAsync(admin, rented.Id, neighbour.Id, OccupancyRelation.Other);

            var units = await (await owner.Client.GetAsync("/api/my/units")).ReadDataAsync<List<MyUnitDto>>();

            Assert.Equal(2, units.Count);
            var asOwner = units.Single(u => u.Id == owner.UnitId);
            Assert.True(asOwner.IsOwner);
            Assert.Null(asOwner.Relation);
            var asTenant = units.Single(u => u.Id == rented.Id);
            Assert.False(asTenant.IsOwner);
            Assert.Equal(OccupancyRelation.Tenant, asTenant.Relation);
            Assert.Equal(Today.AddMonths(3), asTenant.EndDate);
            Assert.Contains("Vecina Conviviente", asTenant.Occupants);

            // como arrendatario no gestiona los ocupantes de esa unidad
            var add = await owner.Client.PostAsJsonAsync("/api/my/occupants", new
            {
                unitId = rented.Id, relation = OccupancyRelation.Family, startDate = Today,
                firstName = "A", lastName = "B", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument()
            });
            Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
        }
    }
}
