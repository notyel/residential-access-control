using AccessControl.Application.Interfaces;
using AccessControl.Application.Services;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Persistence.Interfaces;
using AccessControl.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// 🔎 Mostrar dónde realmente se está conectando
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"🔎 EF se conectará a: {Path.GetFullPath(connectionString.Replace("Data Source=", ""))}");


// Agregar ApplicationDbContext con SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IResidentRepository, ResidentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAngularApp");

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var model = db.Model;

    Console.WriteLine("🔎 EF detecta estas entidades y columnas:");
    foreach (var entity in model.GetEntityTypes())
    {
        Console.WriteLine($"Entidad: {entity.Name}");
        foreach (var prop in entity.GetProperties())
        {
            Console.WriteLine($"   - {prop.Name} ({prop.ClrType.Name})");
        }
    }
}


app.Run();


