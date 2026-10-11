using AccessControl.API.Infrastructure;
using AccessControl.Application.Policies;
using AccessControl.Application.Reports;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Common.DTOs.Parking;
using AccessControl.Common.DTOs.Report;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.Vehicles;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    /// <summary>
    /// Criterios de aceptación del requerimiento (§8), de extremo a extremo por el API, sobre un
    /// conjunto recién creado. Se ejecutan con: dotnet test --filter Category=Aceptacion
    /// </summary>
    [Collection(ApiCollection.Name)]
    [Trait("Category", "Aceptacion")]
    public class AcceptanceTests(ApiFixture api)
    {
        private static DateOnly Today => TestData.Today;

        private static string Range() => $"startDate={DateTime.UtcNow.AddDays(-1):O}&endDate={DateTime.UtcNow.AddMinutes(5):O}";

        [Fact]
        public async Task C8_1_y_8_2_Una_unidad_se_vincula_con_su_propietario_y_con_su_arrendatario_cada_uno_con_su_relacion()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            var unit = await TestData.CreateUnitAsync(admin, "A-101");
            var owner = await TestData.CreatePersonAsync(admin, "Olga", "Propietaria");
            var tenant = await TestData.CreatePersonAsync(admin, "Tomás", "Arrendatario");

            await TestData.AddOwnerAsync(admin, unit.Id, owner.Id);
            await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant);

            var summary = await (await admin.GetAsync($"/api/units/{unit.Id}/summary")).ReadDataAsync<UnitSummaryDto>();
            Assert.Equal("Olga Propietaria", Assert.Single(summary.Owners).PersonName);
            var occupant = Assert.Single(summary.Occupants);
            Assert.Equal(("Tomás Arrendatario", OccupancyRelation.Tenant), (occupant.PersonName, occupant.Relation));
            // propietario no residente: la unidad figura arrendada
            Assert.Equal(UnitOccupancyState.Rented, summary.Unit.OccupancyState);
        }

        [Fact]
        public async Task C8_3_Un_residente_autoriza_porteria_valida_y_el_detalle_muestra_toda_la_trazabilidad()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            var unit = await TestData.CreateUnitAsync(admin);
            var person = await TestData.CreatePersonAsync(admin, "Rosa", "Residente");
            await TestData.AddOccupantAsync(admin, unit.Id, person.Id, OccupancyRelation.Tenant);
            var (_, resident) = await TestData.GrantAccessAsync(api, admin, person.Id, SystemRoles.Resident);
            var guard = await TestData.CreateUserAsync(api, admin, SystemRoles.Guard);

            var authorization = await (await resident.PostAsJsonAsync("/api/my/authorizations", new
            {
                unitId = unit.Id, visitorName = "Víctor Visitante", kind = AuthorizationKind.Single, startDate = Today
            })).ReadDataAsync<AuthorizationDto>();
            var lookup = await (await guard.GetAsync("/api/access/lookup?term=Víctor")).ReadDataAsync<LookupResultDto>();
            Assert.True(Assert.Single(lookup.Authorizations).CanEnterNow);

            var visit = await (await guard.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = unit.Id, authorizationId = authorization.Id,
                newPerson = new { firstName = "Víctor", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).ReadDataAsync<VisitDto>();
            (await guard.PostAsJsonAsync($"/api/access/visits/{visit.Id}/check-out", new { })).EnsureSuccessStatusCode();

            var detail = await (await admin.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>();
            Assert.Equal(authorization.Id, detail.AuthorizationId);
            Assert.Equal(VerificationResult.PriorAuthorization, detail.Verification);
            Assert.Equal("Rosa Residente", detail.AuthorizedByName);
            Assert.Equal(("Gil Guarda", "Gil Guarda"), (detail.EntryOperator, detail.ExitOperator));
            Assert.True(detail.ExitAt > detail.EntryAt);
        }

        [Fact]
        public async Task C8_4_La_verificacion_por_placa_distingue_el_vehiculo_del_propietario_el_del_residente_y_el_del_visitante()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            await TestData.AllowWalkInsAsync(admin);
            var guard = await TestData.CreateUserAsync(api, admin, SystemRoles.Guard);
            var ownerUnit = await TestData.CreateOwnerWithUnitAsync(api, admin);
            var unit = await TestData.CreateUnitAsync(admin);
            var tenant = await TestData.CreatePersonAsync(admin, "Tomás", "Arrendatario");
            await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant);

            (await admin.PostAsJsonAsync("/api/vehicles", new { unitId = ownerUnit.UnitId, responsiblePersonId = ownerUnit.PersonId, plate = "OWN-001", type = "automovil" })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync("/api/vehicles", new { unitId = unit.Id, responsiblePersonId = tenant.Id, plate = "RES-002", type = "motocicleta" })).EnsureSuccessStatusCode();
            await TestData.CheckInAsync(guard, unit.Id, "VIS-003");
            async Task<PlateCheckDto> CheckAsync(string plate) => await (await guard.GetAsync($"/api/vehicles/by-plate/{plate}")).ReadDataAsync<PlateCheckDto>();

            var owner = await CheckAsync("own001");
            Assert.Equal((PlateCheckResult.Authorized, "Olga Propietaria"), (owner.Result, owner.Vehicle!.ResponsibleName));
            var resident = await CheckAsync("RES 002");
            Assert.Equal((PlateCheckResult.Authorized, unit.Label, "Tomás Arrendatario"), (resident.Result, resident.Vehicle!.UnitLabel, resident.Vehicle.ResponsibleName));
            var visitor = await CheckAsync("VIS-003");
            Assert.Equal((PlateCheckResult.VisitInProgress, unit.Label), (visitor.Result, visitor.VisitUnitLabel));
            Assert.Equal(PlateCheckResult.Unknown, (await CheckAsync("NOP999")).Result);
        }

        [Fact]
        public async Task C8_5_y_8_9_El_resumen_de_parqueaderos_coincide_con_lo_registrado_y_las_consultas_se_exportan()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            await TestData.AllowWalkInsAsync(admin);
            var unit = await TestData.CreateUnitAsync(admin);
            async Task<List<ParkingSpaceDto>> SpacesAsync(string prefix, int count, ParkingSpaceType type)
            {
                (await admin.PostAsJsonAsync("/api/parking/spaces/bulk", new { prefix, from = 1, to = count, type })).EnsureSuccessStatusCode();
                return await (await admin.GetAsync($"/api/parking/spaces?type={type}")).ReadDataAsync<List<ParkingSpaceDto>>();
            }
            var privates = await SpacesAsync("P-", 2, ParkingSpaceType.Private);
            var community = await SpacesAsync("C-", 3, ParkingSpaceType.Community);
            var visitors = await SpacesAsync("V-", 5, ParkingSpaceType.Visitor);

            (await admin.PostAsJsonAsync($"/api/parking/spaces/{privates[0].Id}/holder", new { unitId = unit.Id })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync("/api/parking/assignments", new { spaceId = community[0].Id, unitId = unit.Id, startsAt = DateTime.UtcNow, endsAt = DateTime.UtcNow.AddHours(4) })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync($"/api/parking/spaces/{visitors[4].Id}/out-of-service", new { reason = "Pintura" })).EnsureSuccessStatusCode();
            var visit = await (await admin.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = unit.Id, vehiclePlate = "ACE-123", parkingSpaceId = visitors[0].Id,
                newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).ReadDataAsync<VisitDto>();

            var availability = await (await admin.GetAsync("/api/parking/availability")).ReadDataAsync<AvailabilityDto>();
            AvailabilityRowDto Row(ParkingSpaceType type) => availability.ByType.Single(r => r.Type == type);
            Assert.Equal((2, 0, 0, 2, 0), (Row(ParkingSpaceType.Private).Capacity, Row(ParkingSpaceType.Private).Occupied, Row(ParkingSpaceType.Private).Reserved, Row(ParkingSpaceType.Private).Available, Row(ParkingSpaceType.Private).OutOfService));
            Assert.Equal((3, 0, 1, 2, 0), (Row(ParkingSpaceType.Community).Capacity, Row(ParkingSpaceType.Community).Occupied, Row(ParkingSpaceType.Community).Reserved, Row(ParkingSpaceType.Community).Available, Row(ParkingSpaceType.Community).OutOfService));
            Assert.Equal((5, 1, 0, 3, 1), (Row(ParkingSpaceType.Visitor).Capacity, Row(ParkingSpaceType.Visitor).Occupied, Row(ParkingSpaceType.Visitor).Reserved, Row(ParkingSpaceType.Visitor).Available, Row(ParkingSpaceType.Visitor).OutOfService));
            Assert.Equal(10, availability.Total.Capacity);

            // §8.9: accesos filtrados, visitantes presentes y disponibilidad, consultados y exportados
            var accesses = await (await admin.GetAsync($"/api/access-history?{Range()}&plate=ACE123")).ReadDataAsync<PaginatedResultDto<AccessEventDto>>();
            Assert.Equal(visit.EntryEventId, Assert.Single(accesses.Items!).Id);
            var present = await (await admin.GetAsync("/api/visits?status=Inside")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visit.Id, Assert.Single(present.Items!).Id);

            var accessesCsv = await (await admin.GetAsync($"/api/access-history/export?{Range()}&plate=ACE123")).Content.ReadAsStringAsync();
            Assert.Contains("ACE123", accessesCsv);
            var parkingCsv = await (await admin.GetAsync($"/api/reports/{ReportingService.ParkingOccupancy}/export")).Content.ReadAsStringAsync();
            Assert.Contains("Visitantes;5;1;0;3;1", parkingCsv);
            Assert.Contains("Total;10;1;1;7;1", parkingCsv);
        }

        [Fact]
        public async Task C8_6_Todo_endpoint_declara_como_se_protege_y_cada_rol_solo_entra_donde_tiene_permiso()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            var clients = new Dictionary<string, HttpClient> { [SystemRoles.Admin] = admin };
            foreach (var role in new[] { SystemRoles.Guard, SystemRoles.Auditor })
            {
                clients[role] = await TestData.CreateUserAsync(api, admin, role);
            }
            var owner = await TestData.CreateOwnerWithUnitAsync(api, admin);
            clients[SystemRoles.Owner] = owner.Client;
            var tenant = await TestData.CreatePersonAsync(admin);
            await TestData.AddOccupantAsync(admin, owner.UnitId, tenant.Id, OccupancyRelation.Tenant);
            clients[SystemRoles.Resident] = (await TestData.GrantAccessAsync(api, admin, tenant.Id, SystemRoles.Resident)).Client;

            var actions = api.Factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
            Assert.True(actions.Count > 100, "No se encontraron los endpoints del API.");

            // 1. ningún endpoint queda sin protección declarada: un endpoint nuevo sin atributo rompe esta prueba
            var unprotected = actions
                .Where(a => !a.EndpointMetadata.OfType<IAllowAnonymous>().Any() && !a.EndpointMetadata.OfType<AuthorizeAttribute>().Any())
                .Select(a => a.DisplayName)
                .ToList();
            Assert.True(unprotected.Count == 0, "Endpoints sin [HasPermission], [Authorize] ni [AllowAnonymous]: " + string.Join(", ", unprotected));

            // 2. cada rol de sistema llama a todas las consultas: entra solo donde su rol tiene el permiso
            var queries = actions
                .Where(a => a.EndpointMetadata.OfType<Microsoft.AspNetCore.Routing.IHttpMethodMetadata>().Any(m => m.HttpMethods.Contains("GET"))
                    && a.AttributeRouteInfo?.Template is { } template && !template.Contains('{')
                    && !a.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                .ToList();
            Assert.True(queries.Count > 25);

            var failures = new List<string>();
            foreach (var definition in SystemRoles.All)
            {
                bool Has(string permission) => definition.Permissions.Contains(permission)
                    || (permission.EndsWith("_own") && definition.Permissions.Contains(permission[..^4]));

                foreach (var query in queries)
                {
                    var authorize = query.EndpointMetadata.OfType<AuthorizeAttribute>().ToList();
                    // todos los atributos deben cumplirse; dentro de cada [HasPermission] basta un permiso
                    var allowed = authorize.All(a => a switch
                    {
                        HasPermissionAttribute permission => permission.Permissions.Any(Has),
                        { Policy: not null } => false, // solo el superadministrador
                        _ => true
                    });

                    var status = (await clients[definition.Key].GetAsync("/" + query.AttributeRouteInfo!.Template)).StatusCode;
                    var entered = status is not (HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
                    if (entered != allowed)
                    {
                        failures.Add($"{definition.Key} GET /{query.AttributeRouteInfo.Template}: {(int)status}, se esperaba {(allowed ? "acceso" : "403")}");
                    }
                }
            }
            Assert.True(failures.Count == 0, string.Join("\n", failures));

            // 3. sin sesión no se entra a ninguna
            var anonymous = api.Factory.CreateClient();
            foreach (var query in queries)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/" + query.AttributeRouteInfo!.Template)).StatusCode);
            }

            // 4. ni datos de otra unidad ni de otro conjunto
            var (otherAdmin, _, _) = await TestData.CreateComplexAsync(api, "Ajeno");
            var foreign = await TestData.CreateUnitAsync(otherAdmin, "AJENA-1");
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/units/{foreign.Id}")).StatusCode);
            var mine = await (await clients[SystemRoles.Resident].GetAsync("/api/my/units")).ReadDataAsync<List<MyUnitDto>>();
            Assert.Equal(owner.UnitId, Assert.Single(mine).Id);
        }

        [Fact]
        public async Task C8_7_El_menu_de_cada_rol_de_sistema_es_el_definido()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            var owner = await TestData.CreateOwnerWithUnitAsync(api, admin);
            var tenant = await TestData.CreatePersonAsync(admin);
            await TestData.AddOccupantAsync(admin, owner.UnitId, tenant.Id, OccupancyRelation.Tenant);
            var resident = (await TestData.GrantAccessAsync(api, admin, tenant.Id, SystemRoles.Resident)).Client;
            static async Task<string[]> MenuAsync(HttpClient client) =>
                (await (await client.GetAsync("/api/menu")).ReadDataAsync<List<MenuDto>>()).Select(m => m.Name!).ToArray();

            Assert.Equal(new[]
            {
                "Panel", "Unidades", "Propietarios y residentes", "Validar visitantes", "Visitantes", "Autorizaciones", "Verificar vehículos",
                "Vehículos", "Parqueaderos", "Usuarios", "Roles", "Novedades", "Historial de accesos", "Reportes", "Bitácora", "Políticas"
            }, await MenuAsync(admin));
            Assert.Equal(new[]
            {
                "Panel", "Unidades", "Propietarios y residentes", "Validar visitantes", "Visitantes", "Autorizaciones", "Verificar vehículos",
                "Vehículos", "Parqueaderos", "Novedades", "Historial de accesos"
            }, await MenuAsync(await TestData.CreateUserAsync(api, admin, SystemRoles.Guard)));
            Assert.Equal(new[]
            {
                "Panel", "Visitantes", "Mis propiedades", "Mi residencia", "Autorizar visitantes", "Mis vehículos", "Mis parqueaderos", "Historial de accesos"
            }, await MenuAsync(owner.Client));
            Assert.Equal(new[]
            {
                "Panel", "Visitantes", "Mi residencia", "Autorizar visitantes", "Mis vehículos", "Mis parqueaderos", "Historial de accesos"
            }, await MenuAsync(resident));
            Assert.Equal(new[]
            {
                "Panel", "Unidades", "Propietarios y residentes", "Visitantes", "Autorizaciones", "Vehículos", "Parqueaderos", "Usuarios", "Roles",
                "Novedades", "Historial de accesos", "Reportes", "Bitácora", "Políticas"
            }, await MenuAsync(await TestData.CreateUserAsync(api, admin, SystemRoles.Auditor)));
        }

        [Fact]
        public async Task C8_8_Tras_cada_cambio_el_hecho_anterior_sigue_consultable_con_sus_fechas()
        {
            var (admin, _, _) = await TestData.CreateComplexAsync(api, "Aceptacion");
            var unit = await TestData.CreateUnitAsync(admin);
            var first = await TestData.CreatePersonAsync(admin, "Primer", "Dueño");
            var second = await TestData.CreatePersonAsync(admin, "Segundo", "Dueño");
            var tenant = await TestData.CreatePersonAsync(admin, "Tomás", "Arrendatario");
            await TestData.AddOwnerAsync(admin, unit.Id, first.Id);
            var occupancy = await TestData.AddOccupantAsync(admin, unit.Id, tenant.Id, OccupancyRelation.Tenant);

            // autorización de visita y parqueadero privado, antes de los cambios
            var authorization = await (await admin.PostAsJsonAsync("/api/authorizations", new
            {
                unitId = unit.Id, authorizedByPersonId = tenant.Id, visitorName = "Visitante Frecuente", kind = AuthorizationKind.Period, startDate = Today, endDate = Today.AddDays(5)
            })).ReadDataAsync<AuthorizationDto>();
            var space = await (await admin.PostAsJsonAsync("/api/parking/spaces", new { code = "P-01", type = ParkingSpaceType.Private })).ReadDataAsync<ParkingSpaceDto>();
            (await admin.PostAsJsonAsync($"/api/parking/spaces/{space.Id}/holder", new { unitId = unit.Id })).EnsureSuccessStatusCode();

            // los cuatro cambios
            (await admin.PostAsJsonAsync($"/api/units/{unit.Id}/owners/transfer", new { personIds = new[] { second.Id }, date = Today })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync($"/api/authorizations/{authorization.Id}/revoke", new { reason = "Ya no presta el servicio" })).EnsureSuccessStatusCode();
            (await admin.PostAsJsonAsync($"/api/occupancies/{occupancy.Id}/end", new { endDate = Today })).EnsureSuccessStatusCode();
            var otherUnit = await TestData.CreateUnitAsync(admin);
            (await admin.PostAsJsonAsync($"/api/parking/spaces/{space.Id}/holder", new { unitId = otherUnit.Id })).EnsureSuccessStatusCode();

            // los cuatro hechos anteriores siguen ahí, con sus fechas
            var summary = await (await admin.GetAsync($"/api/units/{unit.Id}/summary")).ReadDataAsync<UnitSummaryDto>();
            Assert.Equal("Segundo Dueño", Assert.Single(summary.Owners).PersonName);
            var pastOwner = Assert.Single(summary.PastOwners!);
            Assert.Equal("Primer Dueño", pastOwner.PersonName);
            Assert.NotNull(pastOwner.EndDate);
            var pastTenant = Assert.Single(summary.PastOccupants!);
            Assert.Equal(("Tomás Arrendatario", Today), (pastTenant.PersonName, pastTenant.EndDate));

            var revoked = await (await admin.GetAsync("/api/authorizations?state=Revoked")).ReadDataAsync<PaginatedResultDto<AuthorizationDto>>();
            Assert.Contains(revoked.Items!, a => a.Id == authorization.Id && a.RevokeReason == "Ya no presta el servicio" && a.StartDate == Today);

            var history = (await (await admin.GetAsync($"/api/parking/spaces/{space.Id}")).ReadDataAsync<ParkingSpaceDetailDto>()).History;
            Assert.Contains(history, h => h.UnitLabel == unit.Label && h.Status == ParkingAssignmentStatus.Released && h.EndsAt != null);
            Assert.Contains(history, h => h.UnitLabel == otherUnit.Label && h.Status == ParkingAssignmentStatus.Active);
        }

        [Fact]
        public async Task C8_10_Dos_conjuntos_distintos_operan_cada_uno_con_sus_reglas_sin_cambios_de_codigo()
        {
            // conjunto de casas, sin torres, con visitas a criterio de portería
            var (houses, housesComplex, _) = await TestData.CreateComplexAsync(api, "Casas");
            (await TestData.SetPolicyAsync(houses, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.GuardDiscretion)).EnsureSuccessStatusCode();
            (await TestData.SetPolicyAsync(houses, ComplexPolicies.VisitorDocumentRequired, "false")).EnsureSuccessStatusCode();
            var house = await TestData.CreateUnitAsync(houses, "CASA-12", "casa");

            // conjunto de torres, con autorización previa obligatoria
            var (towers, towersComplex, _) = await TestData.CreateComplexAsync(api, "Torres");
            (await TestData.SetPolicyAsync(towers, ComplexPolicies.VisitsWithoutAuthorization, ComplexPolicies.NotAllowed)).EnsureSuccessStatusCode();
            var tower = await (await towers.PostAsJsonAsync("/api/structures", new { type = "torre", name = "Torre A" })).ReadDataAsync<StructureDto>();
            var apartment = await (await towers.PostAsJsonAsync("/api/units", new { code = "301", type = "apartamento", structureId = tower.Id })).ReadDataAsync<UnitDto>();
            var resident = await TestData.CreatePersonAsync(towers, "Rosa", "Residente");
            await TestData.AddOwnerAsync(towers, apartment.Id, resident.Id);
            await TestData.AddOccupantAsync(towers, apartment.Id, resident.Id, OccupancyRelation.OwnerResident);
            Assert.NotEqual(housesComplex.Id, towersComplex.Id);
            Assert.Contains("Torre A", apartment.Label);
            Assert.Equal("CASA-12", house.Label);

            object WalkIn(Guid unitId, bool withDocument) => new
            {
                unitId,
                newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = withDocument ? "CC" : "", documentNumber = withDocument ? HttpExtensions.UniqueDocument() : "" }
            };

            // en las casas, portería deja entrar sin autorización y sin documento
            var relaxed = await (await houses.PostAsJsonAsync("/api/access/check-in", WalkIn(house.Id, withDocument: false))).ReadDataAsync<VisitDto>();
            Assert.Equal(VerificationResult.GuardApproval, relaxed.Verification);

            // en las torres, lo mismo se rechaza; con autorización previa sí entra
            Assert.Equal(HttpStatusCode.Conflict, (await towers.PostAsJsonAsync("/api/access/check-in", WalkIn(apartment.Id, withDocument: true))).StatusCode);
            var authorization = await (await towers.PostAsJsonAsync("/api/authorizations", new
            {
                unitId = apartment.Id, authorizedByPersonId = resident.Id, visitorName = "Vera Visitante", kind = AuthorizationKind.Single, startDate = Today
            })).ReadDataAsync<AuthorizationDto>();
            var strict = await (await towers.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId = apartment.Id, authorizationId = authorization.Id,
                newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).ReadDataAsync<VisitDto>();
            Assert.Equal(VerificationResult.PriorAuthorization, strict.Verification);

            // cada conjunto solo ve lo suyo
            Assert.Equal(relaxed.Id, Assert.Single((await (await houses.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!).Id);
            Assert.Equal(strict.Id, Assert.Single((await (await towers.GetAsync("/api/visits")).ReadDataAsync<PaginatedResultDto<VisitDto>>()).Items!).Id);
        }
    }
}
