using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Residence;
using AccessControl.Common.DTOs.User;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class ComplexTests(ApiFixture api)
    {
        private const string Password = "ClaveSegura123!";

        private sealed record OtherComplex(ComplexDto Complex, string AdminEmail, Guid ResidenceId);

        /// <summary>Crea un segundo conjunto con su administrador, un propietario y una residencia.</summary>
        private async Task<OtherComplex> CreateComplexAsync()
        {
            var super = await api.SuperAdminAsync();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var adminEmail = $"admin-{suffix}@test.com";

            var complex = await (await super.PostAsJsonAsync("/api/complexes", new
            {
                name = $"Conjunto {suffix}",
                type = ComplexType.Edificio,
                timeZoneId = "America/Mexico_City",
                adminEmail,
                adminFirstName = "Ada",
                adminLastName = "Administradora",
                adminPassword = Password
            })).ReadDataAsync<ComplexDto>();

            var admin = await api.ClientForAsync(adminEmail, Password);
            var owner = await (await admin.PostAsJsonAsync("/api/users", new
            {
                email = $"owner-{suffix}@test.com", firstName = "Otro", lastName = "Dueño",
                apartmentNumber = "1", password = Password, roleKeys = new[] { "owner" }
            })).ReadDataAsync<UserDto>();

            var residenceId = await api.WithDbAsync(async db =>
            {
                var residence = new Domain.Entities.Residence { Identifier = "RES-001", OwnerId = owner.Id };
                db.Residences.Add(residence);
                await db.SaveChangesAsync();
                return residence.Id;
            }, complex.Id);

            return new OtherComplex(complex, adminEmail, residenceId);
        }

        private static object NewVisit(Guid residenceId, string document) => new
        {
            residenceId,
            newPerson = new { firstName = "Vera", lastName = "Visitante", documentType = "CC", documentNumber = document, personType = 0 }
        };

        [Fact]
        public async Task Cada_conjunto_solo_ve_sus_propios_datos()
        {
            var other = await CreateComplexAsync();
            var adminA = await api.AdminAsync();
            var adminB = await api.ClientForAsync(other.AdminEmail, Password);

            // el mismo documento y el mismo identificador de residencia pueden existir en dos conjuntos
            var document = HttpExtensions.UniqueDocument();
            var visitA = await (await adminA.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId, document))).ReadDataAsync<VisitDto>();
            var visitB = await (await adminB.PostAsJsonAsync("/api/visits", NewVisit(other.ResidenceId, document))).ReadDataAsync<VisitDto>();
            Assert.NotEqual(visitA.Person.Id, visitB.Person.Id);

            var listB = await (await adminB.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visitB.Id, Assert.Single(listB.Items!).Id);

            var listA = await (await adminA.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.DoesNotContain(listA.Items!, v => v.Id == visitB.Id);

            // por id, lo de otro conjunto responde como inexistente
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/visits/{visitA.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminA.GetAsync($"/api/visits/{visitB.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.PostAsync($"/api/visits/{visitA.Id}/checkout", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/users/{DataSeeder.OwnerUserId}")).StatusCode);

            var usersB = await (await adminB.GetAsync("/api/users?pageSize=500")).ReadDataAsync<PaginatedResultDto<UserDto>>();
            Assert.Equal(2, usersB.TotalCount);
            Assert.All(usersB.Items!, u => Assert.EndsWith("@test.com", u.Email));

            var residencesB = await (await adminB.GetAsync("/api/residences")).ReadDataAsync<PaginatedResultDto<ResidenceDto>>();
            Assert.Equal(other.ResidenceId, Assert.Single(residencesB.Items!).Id);

            // una visita no se puede registrar contra la residencia de otro conjunto
            var cross = await adminB.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId, HttpExtensions.UniqueDocument()));
            Assert.Equal(HttpStatusCode.BadRequest, cross.StatusCode);
        }

        [Fact]
        public async Task El_superadministrador_trabaja_sobre_el_conjunto_que_elige()
        {
            var other = await CreateComplexAsync();
            var adminB = await api.ClientForAsync(other.AdminEmail, Password);
            var visitB = await (await adminB.PostAsJsonAsync("/api/visits", NewVisit(other.ResidenceId, HttpExtensions.UniqueDocument()))).ReadDataAsync<VisitDto>();

            // sin elegir conjunto no ve datos de ninguno
            var none = await (await (await api.SuperAdminAsync()).GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Empty(none.Items!);

            var onB = await (await (await api.SuperAdminOnAsync(other.Complex.Id)).GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(visitB.Id, Assert.Single(onB.Items!).Id);
        }

        [Fact]
        public async Task El_encabezado_de_conjunto_se_ignora_si_no_es_superadministrador()
        {
            var other = await CreateComplexAsync();
            var adminA = await api.AdminAsync();
            adminA.DefaultRequestHeaders.Add("X-Complex-Id", other.Complex.Id.ToString());

            var residences = await (await adminA.GetAsync("/api/residences")).ReadDataAsync<PaginatedResultDto<ResidenceDto>>();

            Assert.DoesNotContain(residences.Items!, r => r.Id == other.ResidenceId);
            Assert.Contains(residences.Items!, r => r.Id == DataSeeder.ResidenceId);
        }

        [Fact]
        public async Task Solo_el_superadministrador_gestiona_conjuntos()
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await (await api.AdminAsync()).GetAsync("/api/complexes")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await (await api.GuardAsync()).GetAsync("/api/complexes")).StatusCode);

            var list = await (await (await api.SuperAdminAsync()).GetAsync("/api/complexes")).ReadDataAsync<List<ComplexDto>>();
            Assert.Contains(list, c => c.Id == DataSeeder.DefaultComplexId);
        }

        [Fact]
        public async Task Un_conjunto_desactivado_no_permite_iniciar_sesion_a_sus_usuarios()
        {
            var other = await CreateComplexAsync();
            var super = await api.SuperAdminAsync();
            object credentials = new { email = other.AdminEmail, password = Password };

            (await super.PostAsync($"/api/complexes/{other.Complex.Id}/deactivate", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.Anonymous().PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);

            (await super.PostAsync($"/api/complexes/{other.Complex.Id}/activate", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, (await api.Anonymous().PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);
        }

        [Fact]
        public async Task Nombre_duplicado_y_zona_horaria_invalida_se_rechazan()
        {
            var other = await CreateComplexAsync();
            var super = await api.SuperAdminAsync();

            var duplicated = await super.PutAsJsonAsync($"/api/complexes/{other.Complex.Id}",
                new { name = "Conjunto principal", type = ComplexType.Conjunto, timeZoneId = "America/Bogota" });
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);

            var badZone = await super.PutAsJsonAsync($"/api/complexes/{other.Complex.Id}",
                new { name = other.Complex.Name, type = ComplexType.Conjunto, timeZoneId = "Marte/Olympus" });
            Assert.Equal(HttpStatusCode.BadRequest, badZone.StatusCode);
        }
    }
}
