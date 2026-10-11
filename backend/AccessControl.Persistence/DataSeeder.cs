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
        /// <summary>Unidad RES-001 del conjunto inicial (nació como residencia sembrada; la migración UnitsAndPeople la convirtió).</summary>
        public static readonly Guid UnitId = new("8c885dcd-3734-45b6-9b7e-633351852714");

        private static readonly Guid DashboardMenuId = new("9452df48-5b92-40ef-829e-a315c01aadcb");
        private static readonly Guid VisitsMenuId = new("a509d9b3-9c77-4324-bc20-4a004b8c5abf");
        private const string UsersGroup = "Usuarios y roles";

        private static readonly Guid GateMenuId = new("d0a7c9eb-c1d5-46e7-b809-1a2b3c4d5e6f");
        private static readonly Guid AuthorizationsMenuId = new("e1b8dafc-d2e6-47f8-8a1b-2b3c4d5e6f70");
        private static readonly Guid MyAuthorizationsMenuId = new("f2c9eb0d-e3f7-4809-9b2c-3c4d5e6f7081");
        private static readonly Guid VehiclesMenuId = new("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");
        private static readonly Guid VehicleCheckMenuId = new("1b2c3d4e-5f6a-4b7c-9d8e-0f1a2b3c4d5e");
        private static readonly Guid MyVehiclesMenuId = new("2c3d4e5f-6a7b-4c8d-ae9f-1a2b3c4d5e6f");
        private static readonly Guid ParkingMenuId = new("3d4e5f6a-7b8c-4d9e-bfa0-2b3c4d5e6f70");
        private static readonly Guid MyParkingMenuId = new("4e5f6a7b-8c9d-4eaf-80b1-3c4d5e6f7081");
        private static readonly Guid AccessHistoryMenuId = new("5f6a7b8c-9dae-4fb0-91c2-4d5e6f708192");
        private static readonly Guid IncidentsMenuId = new("6a7b8c9d-aebf-40c1-a2d3-5e6f708192a3");
        private static readonly Guid ReportsMenuId = new("7b8c9dae-bfc0-41d2-b3e4-6f708192a3b4");
        private static readonly Guid MyUnitsMenuId = new("b8e5a7c9-afb3-44c5-96e7-f8091a2b3c4d");
        private static readonly Guid MyHomeMenuId = new("c9f6b8da-b0c4-45d6-a7f8-091a2b3c4d5e");
        private static readonly Guid UnitsMenuId = new("a7d4f6b8-9ea2-43b4-85d6-e7f8091a2b3c");
        private static readonly Guid PoliciesMenuId = new("f6c3e5a7-8d91-42a3-b4c5-d6e7f8091a2b");
        private static readonly Guid AuditMenuId = new("e5b2d4f6-7c80-4192-a3b4-c5d6e7f8091a");
        private static readonly Guid UsersMenuId = new("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809");
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
                    Status = UserStatus.Active,
                    SecurityStamp = new Guid("a1000000-0000-4000-8000-000000000001"),
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
                    Status = UserStatus.Active,
                    SecurityStamp = new Guid("a1000000-0000-4000-8000-000000000002"),
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
                    Status = UserStatus.Active,
                    SecurityStamp = new Guid("a1000000-0000-4000-8000-000000000003"),
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
                    Status = UserStatus.Active,
                    SecurityStamp = new Guid("a1000000-0000-4000-8000-000000000004"),
                    CreatedAt = SeedDate
                }
            );

            // Path debe coincidir con las rutas del frontend (front/src/app/modules/access-control)
            modelBuilder.Entity<Menu>().HasData(
                new Menu { Id = DashboardMenuId, Name = "Panel", Path = "/access-control/dashboard", Icon = "LayoutDashboard", Order = 1, Permission = "dashboard.read", CreatedAt = SeedDate },
                new Menu { Id = VisitsMenuId, Name = "Visitantes", Path = "/access-control/visits", Icon = "CarFront", Order = 5, Permission = "visits.read,visits.read_own", CreatedAt = SeedDate },
                new Menu { Id = ResidentsMenuId, Name = "Propietarios y residentes", Path = "/access-control/people", Icon = "Users", Order = 3, Permission = "people.read", CreatedAt = SeedDate },
                new Menu { Id = UnitsMenuId, Name = "Unidades", Path = "/access-control/units", Icon = "Building2", Order = 2, Permission = "units.read", CreatedAt = SeedDate },
                new Menu { Id = GateMenuId, Name = "Validar visitantes", Path = "/access-control/gate", Icon = "DoorOpen", Order = 4, Permission = "access.register", CreatedAt = SeedDate },
                new Menu { Id = AuthorizationsMenuId, Name = "Autorizaciones", Path = "/access-control/authorizations", Icon = "Ticket", Order = 6, Permission = "authorizations.read", CreatedAt = SeedDate },
                new Menu { Id = MyAuthorizationsMenuId, Name = "Autorizar visitantes", Path = "/access-control/my/authorizations", Icon = "Ticket", Order = 12, Permission = "authorizations.write_own", CreatedAt = SeedDate },
                new Menu { Id = VehicleCheckMenuId, Name = "Verificar vehículos", Path = "/access-control/gate/vehicles", Icon = "ScanSearch", Order = 7, Permission = "access.register", CreatedAt = SeedDate },
                new Menu { Id = VehiclesMenuId, Name = "Vehículos", Path = "/access-control/vehicles", Icon = "Car", Order = 8, Permission = "vehicles.read", CreatedAt = SeedDate },
                new Menu { Id = MyVehiclesMenuId, Name = "Mis vehículos", Path = "/access-control/my/vehicles", Icon = "Car", Order = 13, Permission = "vehicles.write_own", CreatedAt = SeedDate },
                new Menu { Id = ParkingMenuId, Name = "Parqueaderos", Path = "/access-control/parking", Icon = "SquareParking", Order = 9, Permission = "parking.read", CreatedAt = SeedDate },
                new Menu { Id = MyParkingMenuId, Name = "Mis parqueaderos", Path = "/access-control/my/parking", Icon = "SquareParking", Order = 14, Permission = "parking.read_own", CreatedAt = SeedDate },
                new Menu { Id = IncidentsMenuId, Name = "Novedades", Path = "/access-control/incidents", Icon = "ClipboardList", Order = 86, Permission = "incidents.write,access.read", Group = "Control", CreatedAt = SeedDate },
                new Menu { Id = AccessHistoryMenuId, Name = "Historial de accesos", Path = "/access-control/access-history", Icon = "History", Order = 87, Permission = "access.read,access.read_own", Group = "Control", CreatedAt = SeedDate },
                new Menu { Id = ReportsMenuId, Name = "Reportes", Path = "/access-control/reports", Icon = "ChartColumn", Order = 88, Permission = "reports.read", Group = "Control", CreatedAt = SeedDate },
                new Menu { Id = MyUnitsMenuId, Name = "Mis propiedades", Path = "/access-control/my/units", Icon = "KeyRound", Order = 10, Permission = "occupancy.write_own", CreatedAt = SeedDate },
                new Menu { Id = MyHomeMenuId, Name = "Mi residencia", Path = "/access-control/my/home", Icon = "House", Order = 11, Permission = "units.read_own", CreatedAt = SeedDate },
                new Menu { Id = UsersMenuId, Name = "Usuarios", Path = "/access-control/users", Icon = "UserCog", Order = 50, Permission = "users.read", Group = UsersGroup, CreatedAt = SeedDate },
                new Menu { Id = RolesMenuId, Name = "Roles", Path = "/access-control/roles", Icon = "ShieldCheck", Order = 51, Permission = "roles.read", Group = UsersGroup, CreatedAt = SeedDate },
                new Menu { Id = AuditMenuId, Name = "Bitácora", Path = "/access-control/audit", Icon = "ScrollText", Order = 90, Permission = "audit.read", Group = "Control", CreatedAt = SeedDate },
                new Menu { Id = PoliciesMenuId, Name = "Políticas", Path = "/access-control/settings/policies", Icon = "Settings", Order = 95, Permission = "settings.read", Group = "Configuración", CreatedAt = SeedDate }
            );

        }
    }
}
