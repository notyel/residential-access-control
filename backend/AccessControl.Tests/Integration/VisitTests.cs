using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Residence;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Persistence;
using AccessControl.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class VisitTests(ApiFixture api)
    {
        private static object NewVisit(Guid residenceId, string? document = null, string? plate = null) => new
        {
            residenceId,
            vehiclePlate = plate,
            newPerson = new
            {
                firstName = "Ana",
                lastName = "Visitante",
                documentType = "CC",
                documentNumber = document ?? HttpExtensions.UniqueDocument(),
                personType = 0
            }
        };

        private async Task<VisitDto> RegisterAsync(HttpClient client, string? plate = null)
        {
            var response = await client.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId, plate: plate));
            return await response.ReadDataAsync<VisitDto>();
        }

        [Fact]
        public async Task El_guarda_busca_residencias_y_registra_una_visita()
        {
            var guard = await api.GuardAsync();

            var residences = await (await guard.GetAsync("/api/residences?search=RES"))
                .ReadDataAsync<PaginatedResultDto<ResidenceDto>>();
            var residence = Assert.Single(residences.Items!, r => r.Id == DataSeeder.ResidenceId);
            Assert.Equal("John Doe", residence.OwnerFullName);

            var visit = await (await guard.PostAsJsonAsync("/api/visits", NewVisit(residence.Id, plate: "abc123")))
                .ReadDataAsync<VisitDto>();

            Assert.Equal("RES-001", visit.ResidenceIdentifier);
            Assert.Equal("ABC123", visit.VehiclePlate);
            Assert.Equal("Guard User", visit.RegisteredByFullName);
            Assert.Null(visit.CheckOut);
        }

        [Fact]
        public async Task Los_datos_opcionales_en_blanco_se_aceptan_y_se_guardan_como_nulos()
        {
            var guard = await api.GuardAsync();

            // así llega el formulario cuando no se diligencian teléfono ni correo
            var response = await guard.PostAsJsonAsync("/api/visits", new
            {
                residenceId = DataSeeder.ResidenceId,
                vehiclePlate = "",
                newPerson = new
                {
                    firstName = "Sin", lastName = "Contacto", documentType = "CC",
                    documentNumber = HttpExtensions.UniqueDocument(), phone = "", email = "", personType = 0
                }
            });

            var visit = await response.ReadDataAsync<VisitDto>();
            Assert.Null(visit.Person.Email);
            Assert.Null(visit.Person.Phone);
            Assert.Null(visit.VehiclePlate);
        }

        [Fact]
        public async Task Residencia_inexistente_responde_400_y_no_500()
        {
            var guard = await api.GuardAsync();

            var response = await guard.PostAsJsonAsync("/api/visits", NewVisit(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("La residencia indicada no existe.", (await response.ReadErrorAsync()).Message);
        }

        [Fact]
        public async Task Sin_residencia_responde_400_con_el_error_del_campo()
        {
            var guard = await api.GuardAsync();

            var response = await guard.PostAsJsonAsync("/api/visits", new { personId = Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains((await response.ReadErrorAsync()).Errors!, e => e.Contains("La residencia es obligatoria."));
        }

        [Fact]
        public async Task Documento_duplicado_como_persona_nueva_responde_409()
        {
            var guard = await api.GuardAsync();
            var document = HttpExtensions.UniqueDocument();
            (await guard.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId, document))).EnsureSuccessStatusCode();

            var response = await guard.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId, document));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task La_salida_se_registra_una_sola_vez_y_guarda_quien_la_marco()
        {
            var guard = await api.GuardAsync();
            var admin = await api.AdminAsync();
            var visit = await RegisterAsync(guard);

            var checkedOut = await (await admin.PostAsync($"/api/visits/{visit.Id}/checkout", null)).ReadDataAsync<VisitDto>();
            Assert.NotNull(checkedOut.CheckOut);
            Assert.Equal(DataSeeder.AdminUserId, checkedOut.CheckedOutById);

            var second = await guard.PostAsync($"/api/visits/{visit.Id}/checkout", null);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            var reloaded = await (await guard.GetAsync($"/api/visits/{visit.Id}")).ReadDataAsync<VisitDto>();
            Assert.Equal(checkedOut.CheckOut, reloaded.CheckOut);
            Assert.Equal(DataSeeder.AdminUserId, reloaded.CheckedOutById);
        }

        [Fact]
        public async Task El_filtro_por_estado_separa_dentro_y_con_salida()
        {
            var guard = await api.GuardAsync();
            var inside = await RegisterAsync(guard);
            var gone = await RegisterAsync(guard);
            (await guard.PostAsync($"/api/visits/{gone.Id}/checkout", null)).EnsureSuccessStatusCode();

            var insideList = await (await guard.GetAsync("/api/visits?status=Inside&pageSize=500"))
                .ReadDataAsync<PaginatedResultDto<VisitDto>>();
            var goneList = await (await guard.GetAsync("/api/visits?status=CheckedOut&pageSize=500"))
                .ReadDataAsync<PaginatedResultDto<VisitDto>>();

            Assert.Contains(insideList.Items!, v => v.Id == inside.Id);
            Assert.DoesNotContain(insideList.Items!, v => v.Id == gone.Id);
            Assert.Contains(goneList.Items!, v => v.Id == gone.Id);
            Assert.DoesNotContain(goneList.Items!, v => v.Id == inside.Id);
        }

        [Fact]
        public async Task La_placa_se_corrige_solo_mientras_no_haya_salida()
        {
            var guard = await api.GuardAsync();
            var visit = await RegisterAsync(guard, "AAA111");

            var updated = await (await guard.PutAsJsonAsync($"/api/visits/{visit.Id}", new { vehiclePlate = "bbb222" }))
                .ReadDataAsync<VisitDto>();
            Assert.Equal("BBB222", updated.VehiclePlate);
            Assert.Equal(visit.Person.Id, updated.Person.Id);

            (await guard.PostAsync($"/api/visits/{visit.Id}/checkout", null)).EnsureSuccessStatusCode();
            var afterCheckout = await guard.PutAsJsonAsync($"/api/visits/{visit.Id}", new { vehiclePlate = "CCC333" });
            Assert.Equal(HttpStatusCode.Conflict, afterCheckout.StatusCode);
        }

        [Fact]
        public async Task Las_visitas_no_se_pueden_borrar()
        {
            var admin = await api.AdminAsync();
            var visit = await RegisterAsync(admin);

            var response = await admin.DeleteAsync($"/api/visits/{visit.Id}");

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}
