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

            var menuId1 = Guid.NewGuid();
            var menuId2 = Guid.NewGuid();
            var menuId3 = Guid.NewGuid();

            modelBuilder.Entity<Menu>().HasData(
                new Menu { Id = menuId1, Name = "Dashboard", Path = "/dashboard", Icon = "LayoutDashboard", Order = 1, CreatedAt = DateTime.UtcNow },
                new Menu { Id = menuId2, Name = "Visits", Path = "/visits", Icon = "CarFront", Order = 2, CreatedAt = DateTime.UtcNow },
                new Menu { Id = menuId3, Name = "Users", Path = "/users", Icon = "Users", Order = 3, CreatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<RoleMenu>().HasData(
                // Admin
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Admin, MenuId = menuId1 },
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Admin, MenuId = menuId2 },
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Admin, MenuId = menuId3 },
                // Guard
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Guard, MenuId = menuId1 },
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Guard, MenuId = menuId2 },
                // Owner
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Owner, MenuId = menuId1 },
                new RoleMenu { Id = Guid.NewGuid(), Role = Role.Owner, MenuId = menuId2 }
            );
        }
    }
}
