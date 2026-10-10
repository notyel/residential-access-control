using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Common.DTOs.Role;
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
    public class RoleTests(ApiFixture api)
    {
        private const string Password = "ClaveSegura123!";

        /// <summary>
        /// Conjunto nuevo para cada prueba: cambiar permisos de un rol de sistema no afecta a las demás.
        /// </summary>
        private async Task<(HttpClient Admin, ComplexDto Complex)> NewComplexAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"admin-{suffix}@roles.test";
            var complex = await (await (await api.SuperAdminAsync()).PostAsJsonAsync("/api/complexes", new
            {
                name = $"Roles {suffix}", type = ComplexType.Conjunto, timeZoneId = "America/Bogota",
                adminEmail = email, adminFirstName = "Ana", adminLastName = "Admin", adminPassword = Password
            })).ReadDataAsync<ComplexDto>();

            return (await api.ClientForAsync(email, Password), complex);
        }

        private async Task<HttpClient> NewUserAsync(HttpClient admin, object roles)
        {
            var email = $"user-{Guid.NewGuid():N}@roles.test";
            var response = await admin.PostAsJsonAsync("/api/users", Merge(new
            {
                email, firstName = "Uno", lastName = "Usuario", password = Password
            }, roles));
            await response.ReadDataAsync<UserDto>();
            return await api.ClientForAsync(email, Password);
        }

        private static Dictionary<string, object?> Merge(object a, object b)
        {
            var result = new Dictionary<string, object?>();
            foreach (var source in new[] { a, b })
            {
                foreach (var property in source.GetType().GetProperties())
                {
                    result[property.Name] = property.GetValue(source);
                }
            }
            return result;
        }

        private static async Task<List<RoleDto>> RolesAsync(HttpClient client) =>
            await (await client.GetAsync("/api/roles")).ReadDataAsync<List<RoleDto>>();

        [Fact]
        public async Task Un_conjunto_nace_con_los_roles_de_sistema_y_sus_permisos()
        {
            var (admin, _) = await NewComplexAsync();

            var roles = await RolesAsync(admin);

            Assert.Equal(SystemRoles.All.Select(r => r.Key).Order(), roles.Select(r => r.SystemKey).Order());
            Assert.All(roles, r => Assert.True(r.IsSystem));

            var auditor = roles.Single(r => r.SystemKey == SystemRoles.Auditor);
            Assert.NotEmpty(auditor.Permissions);
            Assert.All(auditor.Permissions, p => Assert.True(Permissions.IsReadOnly(p), $"{p} no es de solo lectura"));

            var adminRole = roles.Single(r => r.SystemKey == SystemRoles.Admin);
            Assert.Contains(Permissions.RolesManage, adminRole.Permissions);
            Assert.Equal(1, adminRole.UserCount);
        }

        [Fact]
        public async Task Los_usuarios_que_ya_existian_conservan_su_acceso_tras_la_migracion()
        {
            var me = await (await (await api.GuardAsync()).GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();

            Assert.Equal(SystemRoles.Guard, Assert.Single(me.Roles).SystemKey);
            Assert.Contains(Permissions.VisitsCheckout, me.Permissions!);
            Assert.DoesNotContain(Permissions.UsersRead, me.Permissions!);
        }

        [Fact]
        public async Task Quitar_un_permiso_a_un_rol_rige_en_la_siguiente_peticion()
        {
            var (admin, _) = await NewComplexAsync();
            var guard = await NewUserAsync(admin, new { roleKeys = new[] { SystemRoles.Guard } });
            Assert.Equal(HttpStatusCode.OK, (await guard.GetAsync("/api/residences")).StatusCode);

            var role = (await RolesAsync(admin)).Single(r => r.SystemKey == SystemRoles.Guard);
            var update = await admin.PutAsJsonAsync($"/api/roles/{role.Id}", new
            {
                name = role.Name,
                description = role.Description,
                permissions = role.Permissions.Where(p => p != Permissions.ResidencesRead)
            });
            await update.ReadDataAsync<RoleDto>();

            // mismo token: el permiso se resuelve en el servidor, no viaja en él
            Assert.Equal(HttpStatusCode.Forbidden, (await guard.GetAsync("/api/residences")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await guard.GetAsync("/api/visits")).StatusCode);
        }

        [Fact]
        public async Task Un_rol_propio_con_un_solo_permiso_solo_habilita_esa_operacion()
        {
            var (admin, _) = await NewComplexAsync();
            var role = await (await admin.PostAsJsonAsync("/api/roles", new
            {
                name = "Solo usuarios", description = "Consulta usuarios", permissions = new[] { Permissions.UsersRead }
            })).ReadDataAsync<RoleDto>();
            Assert.False(role.IsSystem);

            var user = await NewUserAsync(admin, new { roleIds = new[] { role.Id } });

            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/visits")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/dashboard")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/roles")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/users", new { })).StatusCode);

            var menu = await (await user.GetAsync("/api/menu")).ReadDataAsync<List<MenuDto>>();
            Assert.Equal("Residentes", Assert.Single(menu).Name);
        }

        [Fact]
        public async Task Un_usuario_con_dos_roles_tiene_la_union_de_sus_permisos()
        {
            var (admin, _) = await NewComplexAsync();
            var user = await NewUserAsync(admin, new { roleKeys = new[] { SystemRoles.Guard, SystemRoles.Auditor } });

            var me = await (await user.GetAsync("/api/auth/me")).ReadDataAsync<UserDto>();

            Assert.Equal(2, me.Roles.Count);
            Assert.Contains(Permissions.VisitsRegister, me.Permissions!); // de Portería
            Assert.Contains(Permissions.UsersRead, me.Permissions!);      // de Auditor
            Assert.DoesNotContain(Permissions.UsersManage, me.Permissions!);
        }

        [Fact]
        public async Task El_auditor_consulta_pero_no_modifica()
        {
            var (admin, _) = await NewComplexAsync();
            var auditor = await NewUserAsync(admin, new { roleKeys = new[] { SystemRoles.Auditor } });

            Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/visits")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/roles")).StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsJsonAsync("/api/visits", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsJsonAsync("/api/users", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsJsonAsync("/api/roles", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsync($"/api/visits/{Guid.NewGuid()}/checkout", null)).StatusCode);
        }

        [Fact]
        public async Task Nadie_concede_ni_asigna_permisos_que_no_tiene()
        {
            var (admin, _) = await NewComplexAsync();
            var limited = await (await admin.PostAsJsonAsync("/api/roles", new
            {
                name = "Gestor de roles", permissions = new[] { Permissions.RolesRead, Permissions.RolesManage, Permissions.UsersManage }
            })).ReadDataAsync<RoleDto>();
            var manager = await NewUserAsync(admin, new { roleIds = new[] { limited.Id } });

            // no puede crear un rol con un permiso que él no tiene
            var escalate = await manager.PostAsJsonAsync("/api/roles", new
            {
                name = "Con visitas", permissions = new[] { Permissions.VisitsRegister }
            });
            Assert.Equal(HttpStatusCode.Forbidden, escalate.StatusCode);

            // ni asignarle a alguien el rol de administrador
            var assign = await manager.PostAsJsonAsync("/api/users", new
            {
                email = $"x-{Guid.NewGuid():N}@roles.test", firstName = "X", lastName = "Y", password = Password,
                roleKeys = new[] { SystemRoles.Admin }
            });
            Assert.Equal(HttpStatusCode.Forbidden, assign.StatusCode);

            var unknown = await admin.PostAsJsonAsync("/api/roles", new { name = "Inventado", permissions = new[] { "no.existe" } });
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        }

        [Fact]
        public async Task El_conjunto_no_puede_quedarse_sin_quien_administre_roles()
        {
            var (admin, _) = await NewComplexAsync();
            var adminRole = (await RolesAsync(admin)).Single(r => r.SystemKey == SystemRoles.Admin);

            var removePermission = await admin.PutAsJsonAsync($"/api/roles/{adminRole.Id}", new
            {
                name = adminRole.Name,
                permissions = adminRole.Permissions.Where(p => p != Permissions.RolesManage)
            });
            Assert.Equal(HttpStatusCode.Conflict, removePermission.StatusCode);

            var deactivate = await admin.PostAsync($"/api/roles/{adminRole.Id}/deactivate", null);
            Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
        }

        [Fact]
        public async Task Los_roles_de_sistema_no_se_borran_y_un_rol_inactivo_deja_de_dar_permisos()
        {
            var (admin, _) = await NewComplexAsync();
            var roles = await RolesAsync(admin);
            var guardRole = roles.Single(r => r.SystemKey == SystemRoles.Guard);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/roles/{guardRole.Id}")).StatusCode);

            var custom = await (await admin.PostAsJsonAsync("/api/roles", new
            {
                name = "Temporal", permissions = new[] { Permissions.VisitsRead }
            })).ReadDataAsync<RoleDto>();
            var user = await NewUserAsync(admin, new { roleIds = new[] { custom.Id } });
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/visits")).StatusCode);

            // con usuarios asignados no se borra; desactivarlo le quita los permisos al usuario
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/roles/{custom.Id}")).StatusCode);
            (await admin.PostAsync($"/api/roles/{custom.Id}/deactivate", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/visits")).StatusCode);

            var duplicated = await admin.PostAsJsonAsync("/api/roles", new { name = "temporal", permissions = Array.Empty<string>() });
            Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);
        }

        [Fact]
        public async Task Los_roles_de_un_conjunto_no_se_ven_ni_se_asignan_desde_otro()
        {
            var (adminA, _) = await NewComplexAsync();
            var (adminB, _) = await NewComplexAsync();
            var roleOfB = (await RolesAsync(adminB)).Single(r => r.SystemKey == SystemRoles.Guard);

            Assert.Equal(HttpStatusCode.NotFound, (await adminA.GetAsync($"/api/roles/{roleOfB.Id}")).StatusCode);

            var assign = await adminA.PostAsJsonAsync("/api/users", new
            {
                email = $"x-{Guid.NewGuid():N}@roles.test", firstName = "X", lastName = "Y", password = Password,
                roleIds = new[] { roleOfB.Id }
            });
            Assert.Equal(HttpStatusCode.BadRequest, assign.StatusCode);
        }

        [Fact]
        public async Task El_propietario_sigue_viendo_solo_sus_visitas()
        {
            var owner = await api.OwnerAsync();

            var list = await (await owner.GetAsync("/api/visits?pageSize=500")).ReadDataAsync<PaginatedResultDto<VisitDto>>();

            Assert.All(list.Items!, v => Assert.Equal(DataSeeder.ResidenceId, v.ResidenceId));
        }
    }
}
