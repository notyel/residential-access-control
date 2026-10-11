using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Role;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class AuditTests(ApiFixture api)
    {
        private const string Password = "ClaveSegura123!";

        /// <summary>Conjunto propio: su bitácora solo contiene lo que hace la prueba.</summary>
        private async Task<(HttpClient Admin, string AdminEmail)> NewComplexAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"admin-{suffix}@audit.test";
            await (await (await api.SuperAdminAsync()).PostAsJsonAsync("/api/complexes", new
            {
                name = $"Auditoría {suffix}", type = ComplexType.Conjunto, timeZoneId = "America/Bogota",
                adminEmail = email, adminFirstName = "Ana", adminLastName = "Admin"
            })).ReadDataAsync<ComplexDto>();

            return (await api.ClientForAsync(email, Password), email);
        }

        private static async Task<List<AuditLogDto>> AuditAsync(HttpClient client, string query = "") =>
            (await (await client.GetAsync($"/api/audit?pageSize=200&{query}")).ReadDataAsync<PaginatedResultDto<AuditLogDto>>()).Items!.ToList();

        private static async Task<UserDto> CreateUserAsync(HttpClient admin, string email, string role) =>
            await (await admin.PostAsJsonAsync("/api/users", new
            {
                email, firstName = "Uno", lastName = "Usuario", roleKeys = new[] { role }
            })).ReadDataAsync<UserDto>();

        [Fact]
        public async Task Un_cambio_deja_quien_que_y_cuando_con_valor_anterior_y_nuevo()
        {
            var (admin, _) = await NewComplexAsync();
            var me = await (await admin.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();
            var user = await CreateUserAsync(admin, $"u-{Guid.NewGuid():N}@audit.test", SystemRoles.Guard);
            var before = DateTime.UtcNow.AddSeconds(-5);

            (await admin.PutAsJsonAsync($"/api/users/{user.Id}", new { firstName = "Editado", lastName = "Usuario" })).EnsureSuccessStatusCode();

            var entry = Assert.Single(await AuditAsync(admin, $"entityType=User&action=updated&entityId={user.Id}"));
            Assert.Equal(me.Id, entry.UserId);
            Assert.Equal("Ana Admin", entry.UserName);
            Assert.InRange(entry.OccurredAt, before, DateTime.UtcNow.AddSeconds(5));

            // solo el campo que cambió, con su valor anterior y el nuevo
            using var changes = JsonDocument.Parse(entry.Changes!);
            var property = Assert.Single(changes.RootElement.EnumerateObject());
            Assert.Equal("FirstName", property.Name);
            Assert.Equal("Uno", property.Value.GetProperty("old").GetString());
            Assert.Equal("Editado", property.Value.GetProperty("new").GetString());

            Assert.Contains(await AuditAsync(admin, $"entityType=User&action=created&entityId={user.Id}"), e => e.UserId == me.Id);
        }

        [Fact]
        public async Task Los_cambios_de_permisos_y_de_estado_de_usuarios_quedan_registrados()
        {
            var (admin, _) = await NewComplexAsync();
            var user = await CreateUserAsync(admin, $"u-{Guid.NewGuid():N}@audit.test", SystemRoles.Guard);
            var role = (await (await admin.GetAsync("/api/roles")).ReadDataAsync<List<RoleDto>>()).Single(r => r.SystemKey == SystemRoles.Guard);

            (await admin.PutAsJsonAsync($"/api/roles/{role.Id}", new
            {
                name = role.Name, permissions = role.Permissions.Where(p => p != Permissions.UnitsRead)
            })).EnsureSuccessStatusCode();
            (await admin.PostAsync($"/api/users/{user.Id}/deactivate", null)).EnsureSuccessStatusCode();
            (await admin.PostAsync($"/api/users/{user.Id}/activate", null)).EnsureSuccessStatusCode();
            (await admin.PostAsync($"/api/users/{user.Id}/revoke", null)).EnsureSuccessStatusCode();

            var log = await AuditAsync(admin);

            Assert.Contains(log, e => e.EntityType == "RolePermission" && e.Action == "deleted" && e.Changes!.Contains(Permissions.UnitsRead));
            Assert.Contains(log, e => e.EntityType == "User" && e.Action == "updated" && e.EntityId == user.Id.ToString() && e.Changes!.Contains("Status"));
            Assert.Contains(log, e => e.Action == "user.access_revoked" && e.EntityId == user.Id.ToString());
            Assert.Contains(log, e => e.EntityType == "UserRole" && e.Action == "deleted");
        }

        [Fact]
        public async Task Los_inicios_de_sesion_correctos_y_fallidos_quedan_registrados()
        {
            var (admin, adminEmail) = await NewComplexAsync();

            (await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email = adminEmail, password = "equivocada" })).Dispose();
            (await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email = adminEmail, password = Password })).EnsureSuccessStatusCode();

            var log = await AuditAsync(admin);

            Assert.Contains(log, e => e.Action == "auth.login_failed");
            Assert.Contains(log, e => e.Action == "auth.login");
            Assert.Contains(log, e => e.Action == "auth.invitation_accepted");
        }

        [Fact]
        public async Task La_bitacora_nunca_contiene_contrasenas_sellos_ni_tokens()
        {
            var (admin, adminEmail) = await NewComplexAsync();
            var email = $"u-{Guid.NewGuid():N}@audit.test";
            await CreateUserAsync(admin, email, SystemRoles.Guard);
            await api.ClientForAsync(email, Password);
            (await api.Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
            (await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email = adminEmail, password = "MiClaveSecretaErrada1!" })).Dispose();

            var all = string.Join("\n", (await AuditAsync(admin)).Select(e => e.Changes));

            Assert.DoesNotContain("PasswordHash", all);
            Assert.DoesNotContain("SecurityStamp", all);
            Assert.DoesNotContain("TokenHash", all);
            Assert.DoesNotContain("$2a$", all);
            Assert.DoesNotContain("MiClaveSecretaErrada1!", all);
            Assert.DoesNotContain(Password, all);
        }

        [Fact]
        public async Task Una_operacion_que_falla_no_deja_entrada()
        {
            var (admin, adminEmail) = await NewComplexAsync();
            var before = (await AuditAsync(admin, "entityType=User&action=created")).Count;

            var duplicated = await admin.PostAsJsonAsync("/api/users", new
            {
                email = adminEmail, firstName = "Otro", lastName = "Mismo correo", roleKeys = new[] { SystemRoles.Guard }
            });
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);

            Assert.Equal(before, (await AuditAsync(admin, "entityType=User&action=created")).Count);
        }

        [Fact]
        public async Task La_bitacora_es_de_solo_consulta_exige_permiso_y_respeta_el_conjunto()
        {
            var (adminA, _) = await NewComplexAsync();
            var (adminB, _) = await NewComplexAsync();
            var guardEmail = $"g-{Guid.NewGuid():N}@audit.test";
            var auditorEmail = $"a-{Guid.NewGuid():N}@audit.test";
            var userOfA = await CreateUserAsync(adminA, guardEmail, SystemRoles.Guard);
            await CreateUserAsync(adminA, auditorEmail, SystemRoles.Auditor);

            Assert.Equal(HttpStatusCode.Forbidden, (await (await api.ClientForAsync(guardEmail, Password)).GetAsync("/api/audit")).StatusCode);
            Assert.NotEmpty(await AuditAsync(await api.ClientForAsync(auditorEmail, Password)));

            // lo ocurrido en un conjunto no aparece en la bitácora de otro
            Assert.DoesNotContain(await AuditAsync(adminB), e => e.EntityId == userOfA.Id.ToString());

            var entry = (await AuditAsync(adminA)).First();
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await adminA.DeleteAsync("/api/audit")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminA.DeleteAsync($"/api/audit/{entry.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminA.PutAsJsonAsync($"/api/audit/{entry.Id}", new { })).StatusCode);

            // el superadministrador, sin conjunto elegido, ve las acciones de plataforma
            var platform = await AuditAsync(await api.SuperAdminAsync(), "entityType=Complex&action=created");
            Assert.NotEmpty(platform);
        }
    }
}
