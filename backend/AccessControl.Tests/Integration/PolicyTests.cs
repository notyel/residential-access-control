using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Policy;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using AccessControl.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class PolicyTests(ApiFixture api)
    {
        private const string Password = "ClaveSegura123!";

        private async Task<HttpClient> NewComplexAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"admin-{suffix}@policy.test";
            await (await (await api.SuperAdminAsync()).PostAsJsonAsync("/api/complexes", new
            {
                name = $"Políticas {suffix}", type = ComplexType.Conjunto, timeZoneId = "America/Bogota",
                adminEmail = email, adminFirstName = "Ana", adminLastName = "Admin"
            })).ReadDataAsync<ComplexDto>();
            return await api.ClientForAsync(email, Password);
        }

        private static async Task<List<PolicyDto>> PoliciesAsync(HttpClient client) =>
            await (await client.GetAsync("/api/policies")).ReadDataAsync<List<PolicyDto>>();

        [Fact]
        public async Task Un_conjunto_sin_valores_guardados_usa_los_valores_por_defecto()
        {
            var admin = await NewComplexAsync();

            var policies = await PoliciesAsync(admin);

            Assert.Equal(ComplexPolicies.All.Select(p => p.Key), policies.Select(p => p.Key));
            Assert.All(policies, p => Assert.True(p.IsDefault));
            Assert.Equal("30", policies.Single(p => p.Key == ComplexPolicies.AuthorizationMaxDays).Value);
            Assert.Equal(ComplexPolicies.ResidentConfirmation, policies.Single(p => p.Key == ComplexPolicies.VisitsWithoutAuthorization).Value);
        }

        [Fact]
        public async Task Cambiar_una_politica_persiste_solo_en_ese_conjunto_y_queda_en_la_bitacora()
        {
            var admin = await NewComplexAsync();
            var other = await NewComplexAsync();

            var updated = await (await admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string>
            {
                [ComplexPolicies.VisitsWithoutAuthorization] = ComplexPolicies.GuardDiscretion,
                [ComplexPolicies.AuthorizationMaxDays] = "7"
            })).ReadDataAsync<List<PolicyDto>>();
            Assert.Equal("7", updated.Single(p => p.Key == ComplexPolicies.AuthorizationMaxDays).Value);

            var reloaded = await PoliciesAsync(admin);
            Assert.Equal(ComplexPolicies.GuardDiscretion, reloaded.Single(p => p.Key == ComplexPolicies.VisitsWithoutAuthorization).Value);
            Assert.False(reloaded.Single(p => p.Key == ComplexPolicies.AuthorizationMaxDays).IsDefault);
            Assert.All(await PoliciesAsync(other), p => Assert.True(p.IsDefault));

            // un segundo cambio deja en la bitácora el valor anterior y el nuevo
            (await admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string> { [ComplexPolicies.AuthorizationMaxDays] = "15" })).EnsureSuccessStatusCode();
            var log = (await (await admin.GetAsync("/api/audit?entityType=ComplexPolicy&pageSize=50")).ReadDataAsync<PaginatedResultDto<AuditLogDto>>()).Items!;
            Assert.Contains(log, e => e.Action == "updated" && e.Changes!.Replace(" ", "").Contains("\"old\":\"7\"") && e.Changes.Replace(" ", "").Contains("\"new\":\"15\""));

            // volver al valor por defecto
            var reset = await (await admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string> { [ComplexPolicies.AuthorizationMaxDays] = "30" })).ReadDataAsync<List<PolicyDto>>();
            Assert.True(reset.Single(p => p.Key == ComplexPolicies.AuthorizationMaxDays).IsDefault);
        }

        [Fact]
        public async Task Valores_y_claves_invalidos_se_rechazan()
        {
            var admin = await NewComplexAsync();

            async Task<HttpStatusCode> PutAsync(string key, string value) =>
                (await admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string> { [key] = value })).StatusCode;

            Assert.Equal(HttpStatusCode.BadRequest, await PutAsync(ComplexPolicies.AuthorizationMaxDays, "0"));
            Assert.Equal(HttpStatusCode.BadRequest, await PutAsync(ComplexPolicies.AuthorizationMaxDays, "muchos"));
            Assert.Equal(HttpStatusCode.BadRequest, await PutAsync(ComplexPolicies.MultipleOwners, "quizá"));
            Assert.Equal(HttpStatusCode.BadRequest, await PutAsync(ComplexPolicies.VisitsWithoutAuthorization, "otra"));
            Assert.Equal(HttpStatusCode.BadRequest, await PutAsync("no.existe", "true"));
        }

        [Fact]
        public async Task El_auditor_consulta_las_politicas_pero_no_las_cambia_y_porteria_no_las_ve()
        {
            var admin = await NewComplexAsync();
            async Task<HttpClient> UserAsync(string role)
            {
                var email = $"{role}-{Guid.NewGuid():N}@policy.test";
                await (await admin.PostAsJsonAsync("/api/users", new { email, firstName = "U", lastName = "S", roleKeys = new[] { role } })).ReadDataAsync<UserDto>();
                return await api.ClientForAsync(email, Password);
            }

            var auditor = await UserAsync(SystemRoles.Auditor);
            Assert.NotEmpty(await PoliciesAsync(auditor));
            Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PutAsJsonAsync("/api/policies", new Dictionary<string, string>())).StatusCode);

            var guard = await UserAsync(SystemRoles.Guard);
            Assert.Equal(HttpStatusCode.Forbidden, (await guard.GetAsync("/api/policies")).StatusCode);
        }
    }
}
