using AccessControl.Application.Services;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MysticMind.PostgresEmbed;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AccessControl.Tests.Infrastructure
{
    /// <summary>
    /// Levanta un PostgreSQL real (binarios embebidos, sin Docker), aplica las migraciones y
    /// arranca la API en memoria. Se comparte entre todas las pruebas de integración.
    /// </summary>
    public class ApiFixture : IAsyncLifetime
    {
        public const string SuperAdminEmail = "superadmin@accesscontrol.com";
        public const string SuperAdminPassword = "AdminPass123!";
        public const string AdminEmail = "admin@accesscontrol.com";
        public const string GuardEmail = "guard@accesscontrol.com";
        public const string OwnerEmail = "owner@accesscontrol.com";
        public const string AdminPassword = "AdminPass123!";
        public const string GuardPassword = "GuardPass123!";
        public const string OwnerPassword = "OwnerPass123!";

        private const string PgVersion = "17.4.0";
        private const string TestJwtKey = "clave-de-pruebas-no-usar-en-produccion-0123456789";

        private readonly ConcurrentDictionary<string, string> _tokens = new();
        private PgServer _pg = null!;

        public WebApplicationFactory<Program> Factory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // TEST_DB_CONNECTION permite apuntar a un PostgreSQL ya existente (p. ej. un servicio del CI)
            var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _pg = new PgServer(PgVersion, clearInstanceDirOnStop: true);
                await _pg.StartAsync();
                connectionString = $"Host=localhost;Port={_pg.PgPort};Username=postgres;Password=test;Database=postgres;Include Error Detail=true";
            }

            // Program lee estos valores al arrancar, antes de que la fábrica pueda sobrescribir la configuración
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
            Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            // Program aplica las migraciones y luego reparte roles y permisos de sistema
            Environment.SetEnvironmentVariable("Database__MigrateOnStartup", "true");

            Factory = new WebApplicationFactory<Program>();

            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await Factory.DisposeAsync();
            if (_pg != null)
            {
                await _pg.StopAsync();
                _pg.Dispose();
            }
        }

        public HttpClient Anonymous() => Factory.CreateClient();

        public Task<HttpClient> SuperAdminAsync() => ClientForAsync(SuperAdminEmail, SuperAdminPassword);

        /// <summary>Superadministrador trabajando sobre un conjunto concreto.</summary>
        public async Task<HttpClient> SuperAdminOnAsync(Guid complexId)
        {
            var client = await SuperAdminAsync();
            client.DefaultRequestHeaders.Add("X-Complex-Id", complexId.ToString());
            return client;
        }

        public Task<HttpClient> AdminAsync() => ClientForAsync(AdminEmail, AdminPassword);
        public Task<HttpClient> GuardAsync() => ClientForAsync(GuardEmail, GuardPassword);
        public Task<HttpClient> OwnerAsync() => ClientForAsync(OwnerEmail, OwnerPassword);

        public async Task<HttpClient> ClientForAsync(string email, string password)
        {
            if (!_tokens.TryGetValue(email, out var token))
            {
                var response = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password });
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<ResponseModel<LoginResponseDto>>();
                token = body!.Data!.Token!;
                _tokens[email] = token;
            }

            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>Acceso directo a la base, con la sesión puesta en un conjunto (por defecto, el sembrado).</summary>
        public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action, Guid? complexId = null)
        {
            using var scope = Factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<CurrentUser>().ComplexId = complexId ?? DataSeeder.DefaultComplexId;
            return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }
    }

    [CollectionDefinition(Name)]
    public class ApiCollection : ICollectionFixture<ApiFixture>
    {
        public const string Name = "api";
    }
}
