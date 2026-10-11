using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Common.DTOs.User;
using AccessControl.Common.DTOs.Access;
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
        private static object NewVisit(Guid unitId) => new
        {
            unitId,
            newPerson = new { firstName = "Luis", lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
        };

        /// <summary>Crea otra unidad con su propietario, que además tiene cuenta en la plataforma.</summary>
        private async Task<(string Email, string Password, Guid UnitId)> CreateOtherOwnerAsync()
        {
            var admin = await api.AdminAsync();
            var owner = await TestData.CreateOwnerWithUnitAsync(api, admin);
            return (owner.Email, TestData.Password, owner.UnitId);
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

            Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/access/check-in", NewVisit(DataSeeder.UnitId))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/persons?documentNumber=1")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/units")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/persons/search")).StatusCode);
        }

        [Fact]
        public async Task El_propietario_solo_ve_las_visitas_de_sus_residencias()
        {
            var guard = await api.GuardAsync();
            var (email, password, otherUnitId) = await CreateOtherOwnerAsync();
            var mine = await (await guard.PostAsJsonAsync("/api/access/check-in", NewVisit(DataSeeder.UnitId))).ReadDataAsync<VisitDto>();
            var theirs = await (await guard.PostAsJsonAsync("/api/access/check-in", NewVisit(otherUnitId))).ReadDataAsync<VisitDto>();

            var owner = await api.OwnerAsync();
            var list = await (await owner.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();

            Assert.Contains(list.Items!, v => v.Id == mine.Id);
            Assert.DoesNotContain(list.Items!, v => v.Id == theirs.Id);
            Assert.All(list.Items!, v => Assert.Equal(DataSeeder.UnitId, v.UnitId));

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
            var (email, password, otherUnitId) = await CreateOtherOwnerAsync();
            (await guard.PostAsJsonAsync("/api/access/check-in", NewVisit(otherUnitId))).EnsureSuccessStatusCode();

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
        [InlineData(ApiFixture.AdminEmail, ApiFixture.AdminPassword, 16)]
        [InlineData(ApiFixture.GuardEmail, ApiFixture.GuardPassword, 11)]
        [InlineData(ApiFixture.OwnerEmail, ApiFixture.OwnerPassword, 8)]
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
