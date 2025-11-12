using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccessControl.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Residence> Residences { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<RoleMenu> RoleMenus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Global converters to ensure DateTime kinds are Utc when sent to/received from the database
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (DateTime?) (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null,
                v => v.HasValue ? (DateTime?) DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (clrType == null) continue;

                var entity = modelBuilder.Entity(clrType);
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        entity.Property(property.Name).HasConversion(dateTimeConverter).HasColumnType("timestamp with time zone");
                    }
                    else
                    {
                        entity.Property(property.Name).HasConversion(nullableDateTimeConverter).HasColumnType("timestamp with time zone");
                    }
                }
            }

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.Email).IsUnique();
                b.Property(u => u.Email).IsRequired();
                b.Property(u => u.PasswordHash).IsRequired();
                b.Property(u => u.Role).HasConversion<int>();
            });

            modelBuilder.Entity<Residence>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => r.Identifier);
                b.HasOne(r => r.Owner).WithMany(u => u.Residences).HasForeignKey(r => r.OwnerId);
            });

            modelBuilder.Entity<Visit>(b =>
            {
                b.HasKey(v => v.Id);
                b.HasOne(v => v.Residence).WithMany().HasForeignKey(v => v.ResidenceId);
                b.HasOne(v => v.RegisteredBy).WithMany().HasForeignKey(v => v.RegisteredById);
            });

            modelBuilder.Entity<Menu>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasIndex(m => m.Name).IsUnique();
            });

            modelBuilder.Entity<RoleMenu>(b =>
            {
                b.HasKey(rm => rm.Id);
                b.HasIndex(rm => new { rm.Role, rm.MenuId }).IsUnique();
                b.HasOne(rm => rm.Menu).WithMany(m => m.RoleMenus).HasForeignKey(rm => rm.MenuId);
            });

            base.OnModelCreating(modelBuilder);
            DataSeeder.Seed(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
