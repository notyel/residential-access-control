using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;

namespace AccessControl.Persistence
{
    public static class DataSeeder
    {
        // Todo valor sembrado es fijo: ids, fechas y hashes. Un Guid.NewGuid(), DateTime.UtcNow o
        // BCrypt.HashPassword aquí cambia el modelo en cada compilación e impide aplicar migraciones.
        private static readonly DateTime SeedDate = new DateTime(2025, 11, 21, 0, 0, 0, DateTimeKind.Utc);

        public static readonly Guid DefaultComplexId = new("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f");
        public static readonly Guid SuperAdminUserId = new("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f");
        public static readonly Guid AdminUserId = new("ca682896-4e5f-4e3c-a3f0-f19627a71f93");
        public static readonly Guid OwnerUserId = new("e790170c-4407-4cff-8358-8f6a09a46fad");
        public static readonly Guid GuardUserId = new("1a8553d6-0dee-4c79-aa40-2c41c68cd698");
        public static readonly Guid ResidenceId = new("8c885dcd-3734-45b6-9b7e-633351852714");

        private static readonly Guid DashboardMenuId = new("9452df48-5b92-40ef-829e-a315c01aadcb");
        private static readonly Guid VisitsMenuId = new("a509d9b3-9c77-4324-bc20-4a004b8c5abf");
        private const string UsersGroup = "Usuarios y roles";

        private static readonly Guid RolesMenuId = new("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30");
        private static readonly Guid ResidentsMenuId = new("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142");

        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Complex>().HasData(
                new Complex
                {
                    Id = DefaultComplexId,
                    Name = "Conjunto principal",
                    Type = ComplexType.Conjunto,
                    TimeZoneId = "America/Bogota",
                    CreatedAt = SeedDate
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = SuperAdminUserId,
                    IsSuperAdmin = true,
                    FirstName = "Super",
                    LastName = "Admin",
                    Email = "superadmin@accesscontrol.com",
                    // AdminPass123! (cambiarla tras el primer ingreso)
                    PasswordHash = "$2a$11$NrwVmJEOdhpPYxIjwzhi8e79QrYbaW25WmXZXTOPwq70vuN4Rpl/S",
                    ApartmentNumber = "",
                    CreatedAt = SeedDate
                },
                new User
                {
                    Id = AdminUserId,
                    ComplexId = DefaultComplexId,
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@accesscontrol.com",
                    // AdminPass123!
                    PasswordHash = "$2a$11$NrwVmJEOdhpPYxIjwzhi8e79QrYbaW25WmXZXTOPwq70vuN4Rpl/S",
                    ApartmentNumber = "001",
                    CreatedAt = SeedDate
                },
                new User
                {
                    Id = OwnerUserId,
                    ComplexId = DefaultComplexId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "owner@accesscontrol.com",
                    // OwnerPass123!
                    PasswordHash = "$2a$11$ftyFUyavOWRp2SriTa84WuU8QeoAjQV/9OhC1J3QzojVsjITw/osi",
                    ApartmentNumber = "101",
                    CreatedAt = SeedDate
                },
                new User
                {
                    Id = GuardUserId,
                    ComplexId = DefaultComplexId,
                    FirstName = "Guard",
                    LastName = "User",
                    Email = "guard@accesscontrol.com",
                    // GuardPass123!
                    PasswordHash = "$2a$11$bKkXvLrTLRP4MzUnYuL6Zu1S9g7g.dUolC/DcZxHqApI9YMZco45u",
                    ApartmentNumber = "002",
                    CreatedAt = SeedDate
                }
            );

            modelBuilder.Entity<Residence>().HasData(
                new Residence
                {
                    Id = ResidenceId,
                    ComplexId = DefaultComplexId,
                    Identifier = "RES-001",
                    OwnerId = OwnerUserId,
                    CreatedAt = SeedDate
                }
            );

            // Path debe coincidir con las rutas del frontend (front/src/app/modules/access-control)
            modelBuilder.Entity<Menu>().HasData(
                new Menu { Id = DashboardMenuId, Name = "Panel", Path = "/access-control/dashboard", Icon = "LayoutDashboard", Order = 1, Permission = "dashboard.read", CreatedAt = SeedDate },
                new Menu { Id = VisitsMenuId, Name = "Visitas", Path = "/access-control/visits", Icon = "CarFront", Order = 2, Permission = "visits.read,visits.read_own", CreatedAt = SeedDate },
                new Menu { Id = ResidentsMenuId, Name = "Residentes", Path = "/access-control/residents", Icon = "Users", Order = 3, Permission = "users.read", Group = UsersGroup, CreatedAt = SeedDate },
                new Menu { Id = RolesMenuId, Name = "Roles", Path = "/access-control/roles", Icon = "ShieldCheck", Order = 4, Permission = "roles.read", Group = UsersGroup, CreatedAt = SeedDate }
            );

        }
    }
}
