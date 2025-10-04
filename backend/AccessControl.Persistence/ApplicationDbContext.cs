using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessControl.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<UserBase> Users { get; set; }
        public DbSet<User> AuthUsers { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Resident> Residents { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
            // 🔎 Log para confirmar en tiempo de ejecución qué archivo está usando EF
            var conn = this.Database.GetDbConnection();
            Console.WriteLine($"🔎 EF está usando esta conexión: {conn.ConnectionString}");
            Console.WriteLine($"📂 Ruta absoluta: {Path.GetFullPath(conn.DataSource)}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Agregar logging detallado para ver consultas SQL en la consola
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH inheritance
            modelBuilder.Entity<UserBase>()
                .HasDiscriminator<string>("Type")
                .HasValue<Resident>("Resident")
                .HasValue<User>("User");
                
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.NormalizedName)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<AccessLog>()
                .HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId);
        }
    }
}

