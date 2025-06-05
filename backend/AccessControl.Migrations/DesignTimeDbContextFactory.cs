using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace AccessControl.Migrations
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var currentDir = Directory.GetCurrentDirectory();
            var apiFolder = Path.Combine(Directory.GetParent(currentDir).FullName, "AccessControl.API");
            var dbPath = Path.Combine(apiFolder, "accesscontrol.db");

            var connectionString = $"Data Source={dbPath}";

            optionsBuilder.UseSqlite(connectionString,
                b => b.MigrationsAssembly("AccessControl.Migrations"));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
