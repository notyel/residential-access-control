using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Common.DTOs.User;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Persistence;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class PermissionTests(ApiFixture api)
    {
        private static object NewVisit(Guid residenceId) => new
        {
            residenceId,
            newPerson = new { firstName = "Luis", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument(), personType = 0 }
        };

        /// <summary>Crea un segundo propietario con su residencia, directamente en la base.</summary>
        private async Task<(string Email, string Password, Guid ResidenceId)> CreateOtherOwnerAsync()
        {
            var admin = await api.AdminAsync();
            var email = $"owner-{Guid.NewGuid():N}@test.com";
            const string password = "OtraClave123!";
            var user = await (await admin.PostAsJsonAsync("/api/users", new
            {
                email, firstName = "Otra", lastName = "Propietaria", apartmentNumber = "202", password, roleKeys = new[] { "owner" }
            })).ReadDataAsync<UserDto>();

            var residenceId = await api.WithDbAsync(async db =>
            {
                var residence = new Domain.Entities.Residence { Identifier = $"RES-{Guid.NewGuid():N}"[..12], OwnerId = user.Id };
                db.Residences.Add(residence);
                await db.SaveChangesAsync();
                return residence.Id;
            });

            return (email, password, residenceId);
        }

        [Fact]
        public async Task El_guarda_no_lista_usuarios()
        {
            var guard = await api.GuardAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await guard.GetAsync("/api/users")).StatusCode);
        }

        [Fact]
        public async Task El_propietario_no_registra_visitas_ni_busca_personas_ni_residencias()
        {
            var owner = await api.OwnerAsync();

            Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/persons?documentNumber=1")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/residences")).StatusCode);
        }

        [Fact]
        public async Task El_propietario_solo_ve_las_visitas_de_sus_residencias()
        {
            var guard = await api.GuardAsync();
            var (email, password, otherResidenceId) = await CreateOtherOwnerAsync();
            var mine = await (await guard.PostAsJsonAsync("/api/visits", NewVisit(DataSeeder.ResidenceId))).ReadDataAsync<VisitDto>();
            var theirs = await (await guard.PostAsJsonAsync("/api/visits", NewVisit(otherResidenceId))).ReadDataAsync<VisitDto>();

            var owner = await api.OwnerAsync();
            var list = await (await owner.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();

            Assert.Contains(list.Items!, v => v.Id == mine.Id);
            Assert.DoesNotContain(list.Items!, v => v.Id == theirs.Id);
            Assert.All(list.Items!, v => Assert.Equal(DataSeeder.ResidenceId, v.ResidenceId));

            // por id, una visita ajena responde como si no existiera
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/visits/{mine.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/visits/{theirs.Id}")).StatusCode);

            var other = await api.ClientForAsync(email, password);
            var otherList = await (await other.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();
            Assert.Equal(theirs.Id, Assert.Single(otherList.Items!).Id);
        }

        [Fact]
        public async Task El_panel_responde_a_los_tres_roles_y_al_propietario_solo_con_lo_suyo()
        {
            var guard = await api.GuardAsync();
            var (email, password, otherResidenceId) = await CreateOtherOwnerAsync();
            (await guard.PostAsJsonAsync("/api/visits", NewVisit(otherResidenceId))).EnsureSuccessStatusCode();

            var forAdmin = await (await (await api.AdminAsync()).GetAsync("/api/dashboard")).ReadDataAsync<DashboardDto>();
            var forGuard = await (await guard.GetAsync("/api/dashboard")).ReadDataAsync<DashboardDto>();
            var forOther = await (await (await api.ClientForAsync(email, password)).GetAsync("/api/dashboard")).ReadDataAsync<DashboardDto>();

            Assert.True(forAdmin.TotalVisitsThisMonth >= 1);
            Assert.Equal(forAdmin.TotalVisitsThisMonth, forGuard.TotalVisitsThisMonth);
            Assert.Equal(1, forOther.TotalVisitsThisMonth);
            Assert.Equal(1, forOther.VisitorsInside);
            Assert.Equal(1, forOther.VisitsToday);
            Assert.Single(forOther.LatestVisits!);
        }

        [Theory]
        [InlineData(ApiFixture.AdminEmail, ApiFixture.AdminPassword, 4)]
        [InlineData(ApiFixture.GuardEmail, ApiFixture.GuardPassword, 2)]
        [InlineData(ApiFixture.OwnerEmail, ApiFixture.OwnerPassword, 2)]
        public async Task El_menu_de_cada_rol_apunta_a_rutas_reales_del_frontend(string email, string password, int expected)
        {
            var client = await api.ClientForAsync(email, password);

            var menu = await (await client.GetAsync("/api/menu")).ReadDataAsync<List<MenuDto>>();

            Assert.Equal(expected, menu.Count);
            Assert.All(menu, m => Assert.StartsWith("/access-control/", m.Path));
        }

        [Fact]
        public async Task El_modelo_no_tiene_cambios_pendientes_de_migracion()
        {
            // Con valores no deterministas en el sembrado, EF detecta cambios en cada compilación
            var pending = await api.WithDbAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));
            Assert.False(pending);
        }
    }
}
