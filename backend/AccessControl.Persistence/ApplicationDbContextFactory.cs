using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace AccessControl.Persistence
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        // 'migrations add' no abre conexión; 'database update' sí y exige la variable de entorno
        private const string DesignTimeFallback = "Host=localhost;Database=accesscontrol_design;Username=postgres";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(string.IsNullOrWhiteSpace(connectionString) ? DesignTimeFallback : connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
