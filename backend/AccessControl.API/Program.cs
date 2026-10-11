using AccessControl.API.Infrastructure;
using AccessControl.Application.Access;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Common.DTOs;
using AccessControl.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using AccessControl.Application.Settings;
using AccessControl.Application.Units;
using AccessControl.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AccessControl.Persistence.Generics;

var builder = WebApplication.CreateBuilder(args);

// Desarrollo: la conexión y la clave JWT están en appsettings.Development.json, versionado a propósito para que el
// proyecto arranque sin pasos previos (user-secrets o variables de entorno las reemplazan si existen).
// Producción: ese archivo no se lee; los valores llegan solo por variables de entorno
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Defina la variable de entorno ConnectionStrings__DefaultConnection.");
}

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (!jwtSettings.HasValidKey())
{
    throw new InvalidOperationException(
        $"Falta Jwt:Key o tiene menos de {JwtSettings.MinKeyBytes} bytes. Defina la variable de entorno Jwt__Key.");
}
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    // la clave de desarrollo es pública (está en el repositorio): con ella cualquiera firmaría tokens válidos
    var developmentKey = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build()["Jwt:Key"];
    if (!string.IsNullOrEmpty(developmentKey) && developmentKey == jwtSettings.Key)
    {
        throw new InvalidOperationException("Jwt:Key es la clave pública de desarrollo. Fuera de desarrollo use una clave propia en la variable de entorno Jwt__Key.");
    }
}
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// JWT Auth
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    // La plataforma (conjuntos) es del superadministrador; todo lo demás se protege con [HasPermission]
    options.AddPolicy("SuperAdminOnly", p => p.RequireClaim(AppClaims.SuperAdmin, "true"));
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Los errores de validación de los DTO salen con la misma forma que el resto de respuestas
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrEmpty(entry.Key) ? error.ErrorMessage : $"{entry.Key}: {error.ErrorMessage}"))
                .ToList();
            return new BadRequestObjectResult(
                new ResponseModel<object>(false, "Hay datos que no son válidos.", null, errors));
        };
    });

var rateLimits = builder.Configuration.GetSection(RateLimits.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimits.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = rateLimits.AuthPerMinute, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = async (context, token) =>
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ResponseModel<object>(false, "Demasiados intentos. Espere un minuto e inténtelo de nuevo.", null), token);
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Date/time: la BD guarda UTC, la zona horaria del negocio viene de appsettings
builder.Services.AddOptions<DateTimeSettings>()
    .Bind(builder.Configuration.GetSection(DateTimeSettings.SectionName))
    .Validate(s => TimeZoneInfo.TryFindSystemTimeZoneById(s.TimeZoneId, out _),
        $"{DateTimeSettings.SectionName}:TimeZoneId no es una zona horaria válida.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
// Sesión de la petición: la llena CurrentUserMiddleware y la leen el DbContext y los servicios
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
builder.Services.AddScoped<IDateTimeService, DateTimeService>();

// Repos/Services DI: registrar tus repos y services (por ejemplo IAuthService, IVisitService)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IComplexesService, ComplexesService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IUnitsService, UnitsService>();
builder.Services.AddScoped<IOwnershipService, OwnershipService>();
builder.Services.AddScoped<IOccupancyService, OccupancyService>();
builder.Services.AddScoped<IPeopleService, PeopleService>();
builder.Services.AddScoped<IUnitScope, UnitScope>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<AccessControl.Application.Access.IAuthorizationService>(sp => sp.GetRequiredService<AuthorizationService>());
// lo que se revoca cuando termina una ocupación
builder.Services.AddScoped<IOccupancyEndHandler>(sp => sp.GetRequiredService<AuthorizationService>());
builder.Services.AddScoped<AccessControl.Application.Vehicles.VehicleService>();
builder.Services.AddScoped<AccessControl.Application.Vehicles.IVehicleService>(sp => sp.GetRequiredService<AccessControl.Application.Vehicles.VehicleService>());
builder.Services.AddScoped<IOccupancyEndHandler>(sp => sp.GetRequiredService<AccessControl.Application.Vehicles.VehicleService>());
builder.Services.AddScoped<AccessControl.Application.Parking.IParkingService, AccessControl.Application.Parking.ParkingService>();
builder.Services.AddScoped<IAccessService, AccessService>();
builder.Services.AddScoped<IPersonalDataPolicy, PersonalDataPolicy>();
builder.Services.AddScoped<AccessControl.Application.Reports.ReportingService>();
builder.Services.AddScoped<AccessControl.Application.Reports.IAccessHistoryService>(sp => sp.GetRequiredService<AccessControl.Application.Reports.ReportingService>());
builder.Services.AddScoped<AccessControl.Application.Reports.IReportService>(sp => sp.GetRequiredService<AccessControl.Application.Reports.ReportingService>());
builder.Services.AddScoped<AccessControl.Application.Reports.IIncidentService>(sp => sp.GetRequiredService<AccessControl.Application.Reports.ReportingService>());
builder.Services.AddScoped<AccessControl.Application.Reports.IPrivacyService>(sp => sp.GetRequiredService<AccessControl.Application.Reports.ReportingService>());
if (builder.Configuration.GetValue("Maintenance:Enabled", true))
{
    builder.Services.AddHostedService<DailyMaintenanceService>();
}
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleProvisioner, RoleProvisioner>();
builder.Services.AddScoped<IUserTokenService, UserTokenService>();
// Sin SMTP configurado los correos quedan en el log (de ahí se toman los enlaces en desarrollo)
builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add CORS services
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppPolicy",
        builder =>
        {
            builder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Solo para la base local de desarrollo (perfil "local-db"); en despliegue las migraciones se aplican aparte
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

// Roles de sistema y permisos nuevos del catálogo. Si la base tiene migraciones pendientes se omite:
// las tablas podrían no existir todavía
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
    {
        app.Logger.LogWarning("Hay migraciones pendientes: no se sincronizaron roles ni permisos.");
    }
    else
    {
        await scope.ServiceProvider.GetRequiredService<IRoleProvisioner>().SyncAsync();
    }
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Use CORS policy
app.UseCors("WebAppPolicy");

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
