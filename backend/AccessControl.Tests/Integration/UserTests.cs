using AccessControl.Common.DTOs.User;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class UserTests(ApiFixture api)
    {
        private static object NewUser(string email, string password = "ClaveSegura123!") => new
        {
            email,
            firstName = "Marta",
            lastName = "Residente",
            apartmentNumber = "305",
            password,
            roleKeys = new[] { "owner" }
        };

        private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.com";

        [Fact]
        public async Task El_admin_crea_y_edita_un_residente()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();

            var created = await (await admin.PostAsJsonAsync("/api/users", NewUser(email))).ReadDataAsync<UserDto>();
            Assert.Equal(email, created.Email);
            Assert.Equal("owner", Assert.Single(created.Roles).SystemKey);

            var updated = await (await admin.PutAsJsonAsync($"/api/users/{created.Id}",
                new { firstName = "Marta", lastName = "Residente", apartmentNumber = "410" })).ReadDataAsync<UserDto>();
            Assert.Equal("410", updated.ApartmentNumber);

            var reloaded = await (await admin.GetAsync($"/api/users/{created.Id}")).ReadDataAsync<UserDto>();
            Assert.Equal("410", reloaded.ApartmentNumber);
            Assert.Equal("owner", Assert.Single(reloaded.Roles).SystemKey);

            // el usuario creado puede iniciar sesión con la contraseña indicada
            await api.ClientForAsync(email, "ClaveSegura123!");
        }

        [Fact]
        public async Task Correo_duplicado_responde_409()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            (await admin.PostAsJsonAsync("/api/users", NewUser(email))).EnsureSuccessStatusCode();

            var response = await admin.PostAsJsonAsync("/api/users", NewUser(email.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Datos_invalidos_responden_400_con_el_detalle_por_campo()
        {
            var admin = await api.AdminAsync();

            var response = await admin.PostAsJsonAsync("/api/users", NewUser("no-es-un-correo", "corta"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = (await response.ReadErrorAsync()).Errors!;
            Assert.Contains(errors, e => e.Contains("El correo no tiene un formato válido."));
            Assert.Contains(errors, e => e.Contains("al menos 8 caracteres"));
        }

        [Fact]
        public async Task Editar_un_usuario_inexistente_responde_404()
        {
            var admin = await api.AdminAsync();

            var response = await admin.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}",
                new { firstName = "A", lastName = "B", apartmentNumber = "1" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Solo_el_admin_crea_usuarios()
        {
            var guard = await api.GuardAsync();

            var response = await guard.PostAsJsonAsync("/api/users", NewUser(UniqueEmail()));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Un_usuario_inactivo_no_puede_iniciar_sesion()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            var created = await (await admin.PostAsJsonAsync("/api/users", NewUser(email))).ReadDataAsync<UserDto>();

            await api.WithDbAsync(db => db.Users.Where(u => u.Id == created.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

            var login = await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = "ClaveSegura123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }
    }
}
