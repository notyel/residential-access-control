using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;

namespace AccessControl.Persistence
{
    public static class DataSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var adminUserId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var guardUserId = Guid.NewGuid();
            var residenceId = Guid.NewGuid();

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminUserId,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@accesscontrol.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!"),
                    Role = Role.Admin,
                    ApartmentNumber = "001",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = ownerUserId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "owner@accesscontrol.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("OwnerPass123!"),
                    Role = Role.Owner,
                    ApartmentNumber = "101",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = guardUserId,
                    FirstName = "Guard",
                    LastName = "User",
                    Email = "guard@accesscontrol.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("GuardPass123!"),
                    Role = Role.Guard,
                    ApartmentNumber = "002",
                    CreatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Residence>().HasData(
                new Residence
                {
                    Id = residenceId,
                    Identifier = "RES-001",
                    OwnerId = ownerUserId,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
