using AccessControl.Persistence;
using AccessControl.Persistence.Interfaces;
using AccessControl.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Agregar ApplicationDbContext con SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IResidentRepository, ResidentRepository>();

// Configurar CORS para permitir peticiones desde Angular en puerto 4300
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular4300",
        policy => policy.WithOrigins("http://localhost:4300")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors("AllowAngular4300");

app.UseAuthorization();

app.MapControllers();

app.Run();
