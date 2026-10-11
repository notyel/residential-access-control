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
    public partial class ApplicationDbContext : DbContext
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
        public DbSet<Structure> Structures { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitOwnership> UnitOwnerships { get; set; }
        public DbSet<Occupancy> Occupancies { get; set; }
        public DbSet<CatalogItem> CatalogItems { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<AccessEvent> AccessEvents { get; set; }
        public DbSet<VisitAuthorization> VisitAuthorizations { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleAuthorization> VehicleAuthorizations { get; set; }
        public DbSet<VehicleNote> VehicleNotes { get; set; }
        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<ParkingAssignment> ParkingAssignments { get; set; }
        public DbSet<ParkingOutOfService> ParkingOutOfService { get; set; }
        public DbSet<ParkingNote> ParkingNotes { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ComplexPolicy> ComplexPolicies { get; set; }
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
                b.Property(u => u.Status).HasConversion<int>();
                b.Property(u => u.SecurityStamp).HasDefaultValueSql("gen_random_uuid()");
                b.HasOne(u => u.Complex).WithMany().HasForeignKey(u => u.ComplexId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(u => u.Person).WithMany().HasForeignKey(u => u.PersonId).OnDelete(DeleteBehavior.Restrict);
                // Un usuario sin conjunto (superadministrador) solo es visible sin filtros
                b.HasQueryFilter(u => u.ComplexId == CurrentComplexId);
            });

            modelBuilder.Entity<Structure>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Type).IsRequired().HasMaxLength(50);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(x => new { x.ComplexId, x.ParentId, x.Name }).IsUnique();
            });

            modelBuilder.Entity<Unit>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.Code).IsRequired().HasMaxLength(50);
                b.Property(u => u.Type).IsRequired().HasMaxLength(50);
                b.Property(u => u.Label).IsRequired().HasMaxLength(250);
                b.Property(u => u.Notes).HasMaxLength(500);
                b.Property(u => u.Area).HasPrecision(10, 2);
                // El código identifica la unidad en todo el conjunto, sin importar la estructura
                b.HasIndex(u => new { u.ComplexId, u.Code }).IsUnique();
                b.HasOne(u => u.Structure).WithMany().HasForeignKey(u => u.StructureId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UnitOwnership>(b =>
            {
                b.HasKey(o => o.Id);
                b.Property(o => o.Percentage).HasPrecision(5, 2);
                b.HasOne(o => o.Unit).WithMany(u => u.Ownerships).HasForeignKey(o => o.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(o => o.Person).WithMany().HasForeignKey(o => o.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(o => new { o.UnitId, o.PersonId });
                b.ToTable(t => t.HasCheckConstraint("CK_UnitOwnerships_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
            });

            modelBuilder.Entity<Occupancy>(b =>
            {
                b.HasKey(o => o.Id);
                b.Property(o => o.Relation).HasConversion<int>();
                b.Property(o => o.Status).HasConversion<int>();
                b.Property(o => o.StatusReason).HasMaxLength(300);
                b.HasOne(o => o.Unit).WithMany(u => u.Occupancies).HasForeignKey(o => o.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(o => o.Person).WithMany().HasForeignKey(o => o.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(o => new { o.UnitId, o.PersonId });
                b.ToTable(t => t.HasCheckConstraint("CK_Occupancies_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
            });

            modelBuilder.Entity<CatalogItem>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Catalog).IsRequired().HasMaxLength(50);
                b.Property(c => c.Code).IsRequired().HasMaxLength(50);
                b.Property(c => c.Label).IsRequired().HasMaxLength(100);
                b.HasIndex(c => new { c.ComplexId, c.Catalog, c.Code }).IsUnique();
            });

            modelBuilder.Entity<Visit>(b =>
            {
                b.HasKey(v => v.Id);
                b.Property(v => v.Reason).IsRequired().HasMaxLength(50);
                b.Property(v => v.ReasonNote).HasMaxLength(200);
                b.Property(v => v.Verification).HasConversion<int>();
                b.HasOne(v => v.Vehicle).WithMany().HasForeignKey(v => v.VehicleId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.Unit).WithMany().HasForeignKey(v => v.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.RegisteredBy).WithMany().HasForeignKey(v => v.RegisteredById).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.Person).WithMany().HasForeignKey(v => v.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.Authorization).WithMany().HasForeignKey(v => v.AuthorizationId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.EntryEvent).WithMany().HasForeignKey(v => v.EntryEventId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(v => v.ExitEvent).WithMany().HasForeignKey(v => v.ExitEventId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(v => new { v.ComplexId, v.CreatedAt });
                // "quién está dentro" es la consulta más frecuente de portería
                b.HasIndex(v => new { v.ComplexId, v.ExitEventId });
            });

            modelBuilder.Entity<AccessEvent>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Type).HasConversion<int>();
                b.Property(e => e.PersonKind).HasConversion<int>();
                b.Property(e => e.Verification).HasConversion<int>();
                b.Property(e => e.PersonName).IsRequired().HasMaxLength(200);
                b.Property(e => e.PersonDocument).HasMaxLength(60);
                b.Property(e => e.VehiclePlate).HasMaxLength(15);
                b.Property(e => e.AccessPoint).HasMaxLength(50);
                b.Property(e => e.Notes).HasMaxLength(300);
                b.Property(e => e.Reason).HasMaxLength(300);
                b.Property(e => e.ConfirmationChannel).HasMaxLength(30);
                b.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(e => e.Operator).WithMany().HasForeignKey(e => e.OperatorId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(e => new { e.ComplexId, e.OccurredAt });
                b.HasIndex(e => new { e.UnitId, e.OccurredAt });
                b.HasIndex(e => e.VisitId);
                b.HasIndex(e => e.VoidsEventId);
                b.HasIndex(e => new { e.ComplexId, e.VehiclePlate });
                b.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Restrict);
                // "qué vehículos están dentro" se calcula del último evento de cada vehículo
                b.HasIndex(e => new { e.VehicleId, e.OccurredAt });
            });

            modelBuilder.Entity<Vehicle>(b =>
            {
                b.HasKey(v => v.Id);
                b.Property(v => v.Plate).IsRequired().HasMaxLength(15);
                b.Property(v => v.Type).IsRequired().HasMaxLength(50);
                b.Property(v => v.Brand).HasMaxLength(50);
                b.Property(v => v.Color).HasMaxLength(30);
                b.Property(v => v.Model).HasMaxLength(50);
                b.Property(v => v.Notes).HasMaxLength(300);
                b.Property(v => v.Class).HasConversion<int>();
                // la placa se guarda normalizada: "abc-123" y "ABC 123" son el mismo vehículo
                b.HasIndex(v => new { v.ComplexId, v.Plate }).IsUnique();
            });

            modelBuilder.Entity<VehicleAuthorization>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Status).HasConversion<int>();
                b.Property(a => a.StatusReason).HasMaxLength(300);
                b.HasOne(a => a.Vehicle).WithMany(v => v.Authorizations).HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Unit).WithMany().HasForeignKey(a => a.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Responsible).WithMany().HasForeignKey(a => a.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(a => new { a.UnitId, a.Status });
                // un vehículo no puede tener dos autorizaciones en curso (pendiente, vigente o suspendida)
                b.HasIndex(a => a.VehicleId).IsUnique().HasFilter("\"Status\" IN (0, 1, 2)").HasDatabaseName("UX_VehicleAuthorizations_Open");
                b.ToTable(t => t.HasCheckConstraint("CK_VehicleAuthorizations_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
            });

            modelBuilder.Entity<ParkingSpace>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Code).IsRequired().HasMaxLength(20);
                b.Property(s => s.Location).HasMaxLength(100);
                b.Property(s => s.Notes).HasMaxLength(300);
                b.Property(s => s.Type).HasConversion<int>();
                b.Property(s => s.VehicleKind).HasConversion<int>();
                b.HasIndex(s => new { s.ComplexId, s.Code }).IsUnique();
            });

            // Las restricciones de exclusión (dos asignaciones incompatibles no se cruzan) están en la migración:
            // EF no las modela
            modelBuilder.Entity<ParkingAssignment>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Class).HasConversion<int>();
                b.Property(a => a.Status).HasConversion<int>();
                b.Property(a => a.Notes).HasMaxLength(300);
                b.Property(a => a.ExceptionReason).HasMaxLength(300);
                b.HasOne(a => a.Space).WithMany().HasForeignKey(a => a.SpaceId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Unit).WithMany().HasForeignKey(a => a.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Person).WithMany().HasForeignKey(a => a.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Vehicle).WithMany().HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne<Visit>().WithMany().HasForeignKey(a => a.VisitId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(a => new { a.SpaceId, a.Status, a.StartsAt });
                b.HasIndex(a => new { a.VehicleId, a.Status });
                b.ToTable(t => t.HasCheckConstraint("CK_ParkingAssignments_Dates", "\"EndsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\""));
            });

            modelBuilder.Entity<ParkingOutOfService>(b =>
            {
                b.HasKey(o => o.Id);
                b.Property(o => o.Reason).IsRequired().HasMaxLength(300);
                b.HasOne<ParkingSpace>().WithMany().HasForeignKey(o => o.SpaceId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(o => new { o.SpaceId, o.From });
            });

            modelBuilder.Entity<ParkingNote>(b =>
            {
                b.HasKey(n => n.Id);
                b.Property(n => n.Text).IsRequired().HasMaxLength(300);
                b.HasOne<ParkingSpace>().WithMany().HasForeignKey(n => n.SpaceId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(n => n.Author).WithMany().HasForeignKey(n => n.AuthorId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(n => new { n.SpaceId, n.CreatedAt });
            });

            modelBuilder.Entity<Incident>(b =>
            {
                b.HasKey(i => i.Id);
                b.Property(i => i.Category).IsRequired().HasMaxLength(50);
                b.Property(i => i.Text).IsRequired().HasMaxLength(1000);
                b.Property(i => i.VehiclePlate).HasMaxLength(15);
                b.HasOne(i => i.Unit).WithMany().HasForeignKey(i => i.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(i => i.Person).WithMany().HasForeignKey(i => i.PersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(i => i.Author).WithMany().HasForeignKey(i => i.AuthorId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(i => new { i.ComplexId, i.OccurredAt });
            });

            modelBuilder.Entity<VehicleNote>(b =>
            {
                b.HasKey(n => n.Id);
                b.Property(n => n.Text).IsRequired().HasMaxLength(300);
                b.HasOne<Vehicle>().WithMany().HasForeignKey(n => n.VehicleId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(n => n.Author).WithMany().HasForeignKey(n => n.AuthorId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(n => new { n.VehicleId, n.CreatedAt });
            });

            modelBuilder.Entity<VisitAuthorization>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Kind).HasConversion<int>();
                b.Property(a => a.Status).HasConversion<int>();
                b.Property(a => a.VisitorName).IsRequired().HasMaxLength(200);
                b.Property(a => a.VisitorDocument).HasMaxLength(30);
                b.Property(a => a.Reason).IsRequired().HasMaxLength(50);
                b.Property(a => a.Notes).HasMaxLength(300);
                b.Property(a => a.RevokeReason).HasMaxLength(300);
                b.HasOne(a => a.Unit).WithMany().HasForeignKey(a => a.UnitId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.AuthorizedBy).WithMany().HasForeignKey(a => a.AuthorizedByPersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.VisitorPerson).WithMany().HasForeignKey(a => a.VisitorPersonId).OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(a => new { a.ComplexId, a.EndDate });
                b.HasIndex(a => new { a.UnitId, a.Status });
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

            modelBuilder.Entity<UserToken>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
                b.Property(t => t.Purpose).HasConversion<int>();
                b.HasIndex(t => t.TokenHash).IsUnique();
                b.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ComplexPolicy>(b =>
            {
                b.HasKey(p => new { p.ComplexId, p.Key });
                b.Property(p => p.Key).HasMaxLength(100);
                b.Property(p => p.Value).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<AuditLog>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Action).IsRequired().HasMaxLength(60);
                b.Property(a => a.EntityType).HasMaxLength(100);
                b.Property(a => a.EntityId).HasMaxLength(100);
                b.Property(a => a.UserName).HasMaxLength(200);
                b.Property(a => a.IpAddress).HasMaxLength(64);
                b.Property(a => a.Changes).HasColumnType("jsonb");
                b.HasIndex(a => new { a.ComplexId, a.OccurredAt });
                // sin conjunto en la sesión (superadministrador) se ven las acciones de plataforma
                b.HasQueryFilter(a => a.ComplexId == CurrentComplexId);
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
                        entry.Entity.CreatedById = _currentUser?.UserId;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedById = _currentUser?.UserId;
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

            // después de fijar conjunto y autoría, para que la bitácora los vea ya resueltos
            AuditLogs.AddRange(BuildAuditEntries(DateTime.UtcNow));

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
