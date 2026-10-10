using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Common.DTOs;
using AccessControl.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using AccessControl.Application.Settings;
using AccessControl.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AccessControl.Persistence.Generics;

var builder = WebApplication.CreateBuilder(args);

// Secretos: se leen de user-secrets (desarrollo) o variables de entorno (despliegue), nunca de appsettings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configúrela con 'dotnet user-secrets' o la variable de entorno ConnectionStrings__DefaultConnection.");
}

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (!jwtSettings.HasValidKey())
{
    throw new InvalidOperationException(
        $"Falta Jwt:Key o tiene menos de {JwtSettings.MinKeyBytes} bytes. Configúrela con 'dotnet user-secrets' o la variable de entorno Jwt__Key.");
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
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IResidencesService, ResidencesService>();
builder.Services.AddScoped<IComplexesService, ComplexesService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleProvisioner, RoleProvisioner>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IPersonService, PersonService>();
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

app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
