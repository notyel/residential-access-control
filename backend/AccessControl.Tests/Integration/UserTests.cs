using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Role;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class UserTests(ApiFixture api)
    {
        private const string Password = "ClaveSegura123!";

        private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.com";

        private static object NewUser(string email, params string[] roleKeys) => new
        {
            email,
            firstName = "Marta",
            lastName = "Residente",
            apartmentNumber = "305",
            roleKeys = roleKeys.Length > 0 ? roleKeys : new[] { SystemRoles.Owner }
        };

        private async Task<UserDto> CreateAsync(HttpClient admin, string email, params string[] roleKeys) =>
            await (await admin.PostAsJsonAsync("/api/users", NewUser(email, roleKeys))).ReadDataAsync<UserDto>();

        private Task<HttpResponseMessage> LoginAsync(string email, string password = Password) =>
            api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password });

        /// <summary>Conjunto propio, para las pruebas que cuentan administradores.</summary>
        private async Task<(HttpClient Admin, string AdminEmail)> NewComplexAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"admin-{suffix}@users.test";
            await (await (await api.SuperAdminAsync()).PostAsJsonAsync("/api/complexes", new
            {
                name = $"Usuarios {suffix}", type = ComplexType.Conjunto, timeZoneId = "America/Bogota",
                adminEmail = email, adminFirstName = "Ana", adminLastName = "Admin"
            })).ReadDataAsync<ComplexDto>();

            return (await api.ClientForAsync(email, Password), email);
        }

        [Fact]
        public async Task Un_usuario_nuevo_queda_invitado_y_entra_tras_definir_su_contrasena()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();

            var created = await CreateAsync(admin, email);
            Assert.Equal("Invited", created.Status);
            Assert.Equal(SystemRoles.Owner, Assert.Single(created.Roles).SystemKey);

            // todavía no tiene contraseña: no puede iniciar sesión
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email)).StatusCode);

            var token = api.Emails.LastToken(email, "invitation");
            Assert.NotNull(token);
            var accept = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token, password = Password });
            Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email)).StatusCode);
            Assert.Equal("Active", (await (await admin.GetAsync($"/api/users/{created.Id}")).ReadDataAsync<UserDto>()).Status);

            // el enlace sirve una sola vez
            var again = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token, password = "OtraClaveDistinta1!" });
            Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        }

        [Fact]
        public async Task La_contrasena_debe_cumplir_la_politica()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            await CreateAsync(admin, email);
            var token = api.Emails.LastToken(email, "invitation");

            var tooShort = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token, password = "corta1!" });
            Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

            var common = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token, password = "password123" });
            Assert.Equal(HttpStatusCode.BadRequest, common.StatusCode);

            // un intento rechazado no gasta el enlace
            var ok = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token, password = Password });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        [Fact]
        public async Task Reenviar_la_invitacion_anula_el_enlace_anterior()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            var created = await CreateAsync(admin, email);
            var first = api.Emails.LastToken(email, "invitation");

            (await admin.PostAsync($"/api/users/{created.Id}/resend-invitation", null)).EnsureSuccessStatusCode();
            var second = api.Emails.LastToken(email, "invitation");
            Assert.NotEqual(first, second);

            var old = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token = first, password = Password });
            Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
            var current = await api.Anonymous().PostAsJsonAsync("/api/auth/accept-invitation", new { token = second, password = Password });
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);

            // ya activo, no hay invitación que reenviar
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/users/{created.Id}/resend-invitation", null)).StatusCode);
        }

        [Fact]
        public async Task Desactivar_cierra_la_sesion_abierta_y_reactivar_la_devuelve()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            var created = await CreateAsync(admin, email, SystemRoles.Guard);
            var session = await api.ClientForAsync(email, Password);
            Assert.Equal(HttpStatusCode.OK, (await session.GetAsync("/api/visits")).StatusCode);

            var disabled = await (await admin.PostAsync($"/api/users/{created.Id}/deactivate", null)).ReadDataAsync<UserDto>();
            Assert.Equal("Disabled", disabled.Status);
            Assert.Single(disabled.Roles); // conserva sus roles

            // el mismo token deja de servir en la siguiente petición, y no puede volver a entrar
            Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/visits")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email)).StatusCode);

            var enabled = await (await admin.PostAsync($"/api/users/{created.Id}/activate", null)).ReadDataAsync<UserDto>();
            Assert.Equal("Active", enabled.Status);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email)).StatusCode);
        }

        [Fact]
        public async Task Revocar_quita_los_roles_y_cierra_la_sesion_sin_borrar_al_usuario()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            var created = await CreateAsync(admin, email, SystemRoles.Guard);
            var session = await api.ClientForAsync(email, Password);

            var revoked = await (await admin.PostAsync($"/api/users/{created.Id}/revoke", null)).ReadDataAsync<UserDto>();
            Assert.Empty(revoked.Roles);
            Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/visits")).StatusCode);

            // puede volver a entrar, pero ya no puede hacer nada
            api.ForgetSession(email);
            var again = await api.ClientForAsync(email, Password);
            Assert.Equal(HttpStatusCode.Forbidden, (await again.GetAsync("/api/visits")).StatusCode);
            Assert.Empty((await (await again.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>()).Permissions!);
        }

        [Fact]
        public async Task Nadie_se_desactiva_a_si_mismo_y_siempre_queda_un_administrador()
        {
            var (admin, adminEmail) = await NewComplexAsync();
            var me = await (await admin.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();

            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/users/{me.Id}/deactivate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/users/{me.Id}/revoke", null)).StatusCode);

            // un segundo administrador no puede dejar al conjunto sin el primero si él aún está invitado…
            var secondEmail = UniqueEmail();
            var second = await CreateAsync(admin, secondEmail, SystemRoles.Admin);
            var roles = await (await admin.GetAsync("/api/roles")).ReadDataAsync<List<RoleDto>>();
            var guardRole = roles.Single(r => r.SystemKey == SystemRoles.Guard);

            // …pero una vez activo sí puede quitarle el rol al primero, porque queda él
            var secondClient = await api.ClientForAsync(secondEmail, Password);
            var demote = await secondClient.PutAsJsonAsync($"/api/users/{me.Id}", new
            {
                firstName = me.FirstName, lastName = me.LastName, roleIds = new[] { guardRole.Id }
            });
            Assert.Equal(SystemRoles.Guard, Assert.Single((await demote.ReadDataAsync<UserDto>()).Roles).SystemKey);

            // ahora el segundo es el único administrador: nadie puede desactivarlo ni quitarle el rol
            var third = await CreateAsync(secondClient, UniqueEmail(), SystemRoles.Guard);
            var lastAdmin = await secondClient.PutAsJsonAsync($"/api/users/{second.Id}", new
            {
                firstName = "Marta", lastName = "Residente", roleIds = new[] { guardRole.Id }
            });
            Assert.Equal(HttpStatusCode.Conflict, lastAdmin.StatusCode);
            Assert.NotNull(third);
            Assert.NotNull(adminEmail);
        }

        [Fact]
        public async Task El_admin_edita_datos_y_roles_y_filtra_la_lista()
        {
            var (admin, _) = await NewComplexAsync();
            var email = UniqueEmail();
            var created = await CreateAsync(admin, email, SystemRoles.Owner);
            var roles = await (await admin.GetAsync("/api/roles")).ReadDataAsync<List<RoleDto>>();
            var auditor = roles.Single(r => r.SystemKey == SystemRoles.Auditor);
            var owner = roles.Single(r => r.SystemKey == SystemRoles.Owner);

            var updated = await (await admin.PutAsJsonAsync($"/api/users/{created.Id}", new
            {
                firstName = "Martina", lastName = "Editada", apartmentNumber = "410", roleIds = new[] { auditor.Id, owner.Id }
            })).ReadDataAsync<UserDto>();
            Assert.Equal("Martina", updated.FirstName);
            Assert.Equal(2, updated.Roles.Count);

            // sin roleIds los roles no se tocan
            var sameRoles = await (await admin.PutAsJsonAsync($"/api/users/{created.Id}", new
            {
                firstName = "Martina", lastName = "Editada"
            })).ReadDataAsync<UserDto>();
            Assert.Equal(2, sameRoles.Roles.Count);

            async Task<PaginatedResultDto<UserDto>> ListAsync(string query) =>
                await (await admin.GetAsync($"/api/users?pageSize=100&{query}")).ReadDataAsync<PaginatedResultDto<UserDto>>();

            Assert.Equal(created.Id, Assert.Single((await ListAsync("search=martin")).Items!).Id);
            Assert.Equal(created.Id, Assert.Single((await ListAsync("status=Invited")).Items!).Id);
            Assert.Equal(created.Id, Assert.Single((await ListAsync($"role={auditor.Id}")).Items!).Id);
            Assert.Single((await ListAsync("status=Active")).Items!); // el administrador
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/users?status=Otro")).StatusCode);
        }

        [Fact]
        public async Task Correo_duplicado_responde_409_y_datos_invalidos_400()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            await CreateAsync(admin, email);

            var duplicated = await admin.PostAsJsonAsync("/api/users", NewUser(email.ToUpperInvariant()));
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);

            var invalid = await admin.PostAsJsonAsync("/api/users", NewUser("no-es-un-correo"));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Contains((await invalid.ReadErrorAsync()).Errors!, e => e.Contains("El correo no tiene un formato válido."));

            var noRoles = await admin.PostAsJsonAsync("/api/users", new { email = UniqueEmail(), firstName = "A", lastName = "B" });
            Assert.Equal(HttpStatusCode.BadRequest, noRoles.StatusCode);

            var missing = await admin.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new { firstName = "A", lastName = "B" });
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task Solo_quien_administra_usuarios_los_crea_o_desactiva()
        {
            var guard = await api.GuardAsync();

            Assert.Equal(HttpStatusCode.Forbidden, (await guard.PostAsJsonAsync("/api/users", NewUser(UniqueEmail()))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await guard.PostAsync($"/api/users/{Guid.NewGuid()}/deactivate", null)).StatusCode);
        }

        [Fact]
        public async Task Cinco_intentos_fallidos_bloquean_el_inicio_de_sesion_aunque_luego_la_clave_sea_correcta()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            await CreateAsync(admin, email, SystemRoles.Guard);
            await api.AcceptPendingInvitationAsync(email, Password);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "clave-equivocada")).StatusCode);
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email)).StatusCode);

            // pasado el bloqueo vuelve a entrar
            await api.WithDbAsync(db => db.Users.Where(u => u.Email == email)
                .ExecuteUpdateAsync(set => set.SetProperty(u => u.LockedUntil, DateTime.UtcNow.AddMinutes(-1))));
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email)).StatusCode);
        }

        [Fact]
        public async Task Cerrar_sesion_invalida_el_token_en_el_servidor()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            await CreateAsync(admin, email, SystemRoles.Guard);
            var session = await api.ClientForAsync(email, Password);
            api.ForgetSession(email);

            Assert.Equal(HttpStatusCode.OK, (await session.PostAsync("/api/auth/logout", null)).StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/visits")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email)).StatusCode);
        }

        [Fact]
        public async Task Recuperar_la_contrasena_usa_un_enlace_de_un_solo_uso_y_cierra_las_sesiones()
        {
            var admin = await api.AdminAsync();
            var email = UniqueEmail();
            await CreateAsync(admin, email, SystemRoles.Guard);
            var session = await api.ClientForAsync(email, Password);

            // la respuesta es la misma exista o no la cuenta
            var known = await api.Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email });
            var unknown = await api.Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email = UniqueEmail() });
            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());

            var token = api.Emails.LastToken(email, "reset-password");
            Assert.NotNull(token);
            const string newPassword = "NuevaClaveSegura456!";
            var reset = await api.Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { token, password = newPassword });
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/visits")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, newPassword)).StatusCode);

            var reuse = await api.Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { token, password = "OtraClaveMas789!" });
            Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        }
    }
}
