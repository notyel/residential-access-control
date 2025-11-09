using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Residence> Residences { get; set; }
        public DbSet<Visit> Visits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
                b.Property(v => v.CheckIn).HasDefaultValueSql("NOW()");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
