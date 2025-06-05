using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<UserBase> Users { get; set; }
        public DbSet<Resident> Residents { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH inheritance
            modelBuilder.Entity<UserBase>()
                .HasDiscriminator<string>("Type")
                .HasValue<Resident>("Resident");

            modelBuilder.Entity<AccessLog>()
                .HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId);
        }
    }
}

