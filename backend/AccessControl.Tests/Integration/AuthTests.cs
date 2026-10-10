using AccessControl.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace AccessControl.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class AuthTests(ApiFixture api)
    {
        [Fact]
        public async Task Login_con_credenciales_correctas_devuelve_token()
        {
            var response = await api.Anonymous().PostAsJsonAsync("/api/auth/login",
                new { email = ApiFixture.AdminEmail, password = ApiFixture.AdminPassword });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_con_contrasena_incorrecta_responde_401()
        {
            var response = await api.Anonymous().PostAsJsonAsync("/api/auth/login",
                new { email = ApiFixture.AdminEmail, password = "incorrecta" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Sin_token_no_se_accede_a_un_endpoint_protegido()
        {
            var response = await api.Anonymous().GetAsync("/api/visits");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
