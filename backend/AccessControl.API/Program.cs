using AccessControl.Application.Interfaces;
using AccessControl.Application.Services;
using AccessControl.Domain.Enums;
using AccessControl.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Auth
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GuardOnly", p => p.RequireRole(Role.Guard.ToString()));
    options.AddPolicy("AdminOnly", p => p.RequireRole(Role.Admin.ToString()));
    options.AddPolicy("OwnerOnly", p => p.RequireRole(Role.Owner.ToString()));
});

builder.Services.AddControllers();

// Repos/Services DI: registrar tus repos y services (por ejemplo IAuthService, IVisitService)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVisitService, VisitService>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppPolicy",
        builder =>
        {
            builder.WithOrigins("http://localhost:4400")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseHttpsRedirection();

// Use CORS policy
app.UseCors("WebAppPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
