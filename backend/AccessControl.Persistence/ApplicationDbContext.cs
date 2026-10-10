using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccessControl.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentUser? _currentUser;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts, ICurrentUser? currentUser = null) : base(opts)
        {
            _currentUser = currentUser;
        }

        /// <summary>
        /// Conjunto de la petición en curso; lo leen los filtros globales en cada consulta.
        /// Null = no devuelve ninguna fila de entidades con conjunto (seguro por defecto).
        /// </summary>
        public Guid? CurrentComplexId => _currentUser?.ComplexId;

        public DbSet<Complex> Complexes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Residence> Residences { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AppliedPermissionDefault> AppliedPermissionDefaults { get; set; }

        // Every DateTime/DateTime? column is stored as UTC in "timestamp with time zone"
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>()
                .HaveColumnType("timestamp with time zone");
        }

        // Local values are converted to UTC (not just relabeled); values without Kind are assumed to already be UTC
        private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
        {
            public UtcDateTimeConverter() : base(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Complex>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).IsRequired().HasMaxLength(150);
                b.Property(c => c.TimeZoneId).IsRequired().HasMaxLength(100);
                b.Property(c => c.Address).HasMaxLength(250);
                b.Property(c => c.Type).HasConversion<int>();
                b.HasIndex(c => c.Name).IsUnique();
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                // El correo identifica la cuenta al iniciar sesión: es único en toda la plataforma
                b.HasIndex(u => u.Email).IsUnique();
                b.Property(u => u.Email).IsRequired();
                b.Property(u => u.PasswordHash).IsRequired();
                b.HasOne(u => u.Complex).WithMany().HasForeignKey(u => u.ComplexId).OnDelete(DeleteBehavior.Restrict);
                // Un usuario sin conjunto (superadministrador) solo es visible sin filtros
                b.HasQueryFilter(u => u.ComplexId == CurrentComplexId);
            });

            modelBuilder.Entity<Residence>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => new { r.ComplexId, r.Identifier }).IsUnique();
                b.Property(r => r.Identifier).IsRequired();
                b.HasOne(r => r.Owner).WithMany(u => u.Residences).HasForeignKey(r => r.OwnerId);
            });

            modelBuilder.Entity<Visit>(b =>
            {
                b.HasKey(v => v.Id);
                b.HasOne(v => v.Residence).WithMany().HasForeignKey(v => v.ResidenceId);
                b.HasOne(v => v.RegisteredBy).WithMany().HasForeignKey(v => v.RegisteredById);
                b.HasOne(v => v.Person).WithMany().HasForeignKey(v => v.PersonId);
                b.HasOne(v => v.CheckedOutBy).WithMany().HasForeignKey(v => v.CheckedOutById);
                b.HasIndex(v => new { v.ComplexId, v.CreatedAt });
            });

            modelBuilder.Entity<Person>(b =>
            {
                b.HasKey(p => p.Id);
                b.HasIndex(p => new { p.ComplexId, p.DocumentNumber }).IsUnique();
            });

            modelBuilder.Entity<Menu>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasIndex(m => m.Name).IsUnique();
                b.Property(m => m.Permission).IsRequired().HasMaxLength(300);
                b.Property(m => m.Group).HasMaxLength(100);
            });

            modelBuilder.Entity<Role>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Name).IsRequired().HasMaxLength(100);
                b.Property(r => r.Description).HasMaxLength(300);
                b.Property(r => r.SystemKey).HasMaxLength(30);
                b.HasIndex(r => new { r.ComplexId, r.Name }).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(b =>
            {
                b.HasKey(rp => new { rp.RoleId, rp.Permission });
                b.Property(rp => rp.Permission).HasMaxLength(100);
                b.HasOne(rp => rp.Role).WithMany(r => r.Permissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
                // siguen la visibilidad de su rol
                b.HasQueryFilter(rp => rp.Role.ComplexId == CurrentComplexId);
            });

            modelBuilder.Entity<UserRole>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.RoleId });
                b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
                b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
                b.HasQueryFilter(ur => ur.Role.ComplexId == CurrentComplexId);
            });

            modelBuilder.Entity<AppliedPermissionDefault>(b =>
            {
                b.HasKey(a => a.Key);
                b.Property(a => a.Key).HasMaxLength(100);
            });

            ApplyComplexScope(modelBuilder);

            base.OnModelCreating(modelBuilder);
            DataSeeder.Seed(modelBuilder);
        }

        /// <summary>
        /// Toda entidad IComplexScoped queda atada a su conjunto (FK) y filtrada por el conjunto
        /// actual, sin depender de que cada consulta lo recuerde.
        /// </summary>
        private void ApplyComplexScope(ModelBuilder modelBuilder)
        {
            var configure = typeof(ApplicationDbContext)
                .GetMethod(nameof(ConfigureComplexScoped), BindingFlags.Instance | BindingFlags.NonPublic)!;

            var scopedTypes = modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => typeof(IComplexScoped).IsAssignableFrom(t))
                .ToList();

            foreach (var type in scopedTypes)
            {
                configure.MakeGenericMethod(type).Invoke(this, new object[] { modelBuilder });
            }
        }

        private void ConfigureComplexScoped<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IComplexScoped
        {
            modelBuilder.Entity<TEntity>(b =>
            {
                b.HasOne<Complex>().WithMany().HasForeignKey(e => e.ComplexId).OnDelete(DeleteBehavior.Restrict);
                b.HasQueryFilter(e => e.ComplexId == CurrentComplexId);
            });
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

            foreach (var entry in ChangeTracker.Entries<IComplexScoped>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.ComplexId == Guid.Empty)
                    {
                        // Lo normal: el conjunto sale de la sesión, nunca de lo que envíe el cliente
                        entry.Entity.ComplexId = CurrentComplexId
                            ?? throw new InvalidOperationException(
                                $"No se puede guardar {entry.Entity.GetType().Name} sin un conjunto en la sesión.");
                    }
                    else if (entry.Entity.ComplexId != CurrentComplexId && _currentUser is { IsSuperAdmin: false, UserId: not null })
                    {
                        // Fijar otro conjunto a mano solo lo hacen el aprovisionamiento interno y el superadministrador
                        throw new InvalidOperationException("No se puede guardar en un conjunto distinto al de la sesión.");
                    }
                }
                else if (entry.State == EntityState.Modified && entry.Property(nameof(IComplexScoped.ComplexId)).IsModified)
                {
                    throw new InvalidOperationException("Una entidad no puede cambiar de conjunto.");
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
