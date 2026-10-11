using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Enums;
using System.Net.Http.Json;

namespace AccessControl.Tests.Infrastructure
{
    /// <summary>Datos de prueba creados por el API, como lo haría un usuario.</summary>
    public static class TestData
    {
        public const string Password = "ClaveSegura123!";

        public sealed record NewComplex(HttpClient Admin, ComplexDto Complex, string AdminEmail);

        public sealed record OwnerWithUnit(string Email, Guid UnitId, Guid PersonId, HttpClient Client);

        public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

        /// <summary>Conjunto nuevo con su administrador ya activo: aísla la prueba de las demás.</summary>
        public static async Task<NewComplex> CreateComplexAsync(ApiFixture api, string prefix = "Conjunto")
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"admin-{suffix}@test.com";
            var complex = await (await (await api.SuperAdminAsync()).PostAsJsonAsync("/api/complexes", new
            {
                name = $"{prefix} {suffix}", type = ComplexType.Conjunto, timeZoneId = "America/Bogota",
                adminEmail = email, adminFirstName = "Ana", adminLastName = "Admin"
            })).ReadDataAsync<ComplexDto>();

            return new NewComplex(await api.ClientForAsync(email, Password), complex, email);
        }

        /// <summary>Usuario nuevo del conjunto con ese rol de sistema, ya activo.</summary>
        public static async Task<HttpClient> CreateUserAsync(ApiFixture api, HttpClient admin, string roleKey, string firstName = "Gil", string lastName = "Guarda")
        {
            var email = $"{roleKey}-{Guid.NewGuid():N}@test.com";
            await (await admin.PostAsJsonAsync("/api/users", new { email, firstName, lastName, roleKeys = new[] { roleKey } })).ReadDataAsync<UserDto>();
            return await api.ClientForAsync(email, Password);
        }

        public static Task<HttpResponseMessage> SetPolicyAsync(HttpClient admin, string key, string value) =>
            admin.PutAsJsonAsync("/api/policies", new Dictionary<string, string> { [key] = value });

        /// <summary>Deja las visitas sin autorización a criterio de portería.</summary>
        public static async Task AllowWalkInsAsync(HttpClient admin) =>
            (await SetPolicyAsync(admin, "visits.without_authorization", "guard_discretion")).EnsureSuccessStatusCode();

        /// <summary>Ingreso de un visitante nuevo, sin autorización previa.</summary>
        public static async Task<VisitDto> CheckInAsync(HttpClient client, Guid unitId, string? plate = null, string firstName = "Vera")
        {
            return await (await client.PostAsJsonAsync("/api/access/check-in", new
            {
                unitId, vehiclePlate = plate,
                newPerson = new { firstName, lastName = "Visitante", documentType = "CC", documentNumber = HttpExtensions.UniqueDocument() }
            })).ReadDataAsync<VisitDto>();
        }

        public static async Task<UnitDto> CreateUnitAsync(HttpClient admin, string? code = null, string type = "apartamento")
        {
            return await (await admin.PostAsJsonAsync("/api/units", new
            {
                code = code ?? "U-" + Guid.NewGuid().ToString("N")[..6], type
            })).ReadDataAsync<UnitDto>();
        }

        public static async Task<PersonDto> CreatePersonAsync(HttpClient client, string firstName = "Paula", string lastName = "Persona")
        {
            return await (await client.PostAsJsonAsync("/api/persons", new
            {
                firstName, lastName, documentType = "CC", documentNumber = HttpExtensions.UniqueDocument()
            })).ReadDataAsync<PersonDto>();
        }

        public static async Task<OwnershipDto> AddOwnerAsync(HttpClient admin, Guid unitId, Guid personId, DateOnly? start = null)
        {
            return await (await admin.PostAsJsonAsync($"/api/units/{unitId}/owners", new
            {
                personId, startDate = start ?? Today.AddYears(-1)
            })).ReadDataAsync<OwnershipDto>();
        }

        public static async Task<OccupancyDto> AddOccupantAsync(HttpClient admin, Guid unitId, Guid personId,
            OccupancyRelation relation, DateOnly? start = null, DateOnly? end = null)
        {
            return await (await admin.PostAsJsonAsync("/api/occupancies", new
            {
                unitId, personId, relation, startDate = start ?? Today.AddMonths(-1), endDate = end
            })).ReadDataAsync<OccupancyDto>();
        }

        /// <summary>Le da cuenta a la persona (rol propietario o residente) y devuelve su sesión.</summary>
        public static async Task<(string Email, HttpClient Client)> GrantAccessAsync(ApiFixture api, HttpClient admin, Guid personId, string roleKey)
        {
            var email = $"{roleKey}-{Guid.NewGuid():N}@test.com";
            await (await admin.PostAsJsonAsync($"/api/persons/{personId}/grant-access", new { email, roleKey })).ReadDataAsync<UserDto>();
            return (email, await api.ClientForAsync(email, Password));
        }

        /// <summary>Unidad nueva con un propietario vigente que tiene cuenta.</summary>
        public static async Task<OwnerWithUnit> CreateOwnerWithUnitAsync(ApiFixture api, HttpClient admin)
        {
            var unit = await CreateUnitAsync(admin);
            var person = await CreatePersonAsync(admin, "Olga", "Propietaria");
            await AddOwnerAsync(admin, unit.Id, person.Id);
            var (email, client) = await GrantAccessAsync(api, admin, person.Id, "owner");
            return new OwnerWithUnit(email, unit.Id, person.Id, client);
        }
    }
}
