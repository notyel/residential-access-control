using System;
using System.Collections.Generic;
using System.Linq;

namespace AccessControl.Application.Security
{
    public sealed record PermissionDefinition(string Key, string Module, string Label);

    public sealed record SystemRoleDefinition(string Key, string Name, string Description, IReadOnlySet<string> Permissions);

    /// <summary>
    /// Catálogo de permisos, con la forma modulo.accion. Es la única lista válida: los roles
    /// guardan estas claves y los endpoints las exigen con [HasPermission]. Cada historia nueva
    /// añade aquí las suyas y decide a qué roles de sistema se les conceden por defecto.
    /// </summary>
    public static class Permissions
    {
        public const string DashboardRead = "dashboard.read";

        public const string VisitsRead = "visits.read";
        /// <summary>Solo las visitas de las residencias propias.</summary>
        public const string VisitsReadOwn = "visits.read_own";
        /// <summary>Operar la portería: validar, registrar ingresos y salidas, rechazar.</summary>
        public const string AccessRegister = "access.register";
        /// <summary>Permitir un ingreso sin autorización válida, dejando motivo y responsable.</summary>
        public const string AccessException = "access.exception";
        /// <summary>Anular un evento de acceso registrado por error.</summary>
        public const string AccessVoid = "access.void";

        /// <summary>Historial de todos los eventos de acceso del conjunto.</summary>
        public const string AccessRead = "access.read";
        /// <summary>Historial de accesos de las unidades propias, durante el vínculo (portal).</summary>
        public const string AccessReadOwn = "access.read_own";
        /// <summary>Registrar novedades del turno.</summary>
        public const string IncidentsWrite = "incidents.write";

        public const string ReportsRead = "reports.read";
        public const string ReportsExport = "reports.export";

        public const string AuthorizationsRead = "authorizations.read";
        public const string AuthorizationsWrite = "authorizations.write";
        /// <summary>Autorizar visitas a las unidades propias (portal).</summary>
        public const string AuthorizationsWriteOwn = "authorizations.write_own";

        public const string VehiclesRead = "vehicles.read";
        public const string VehiclesWrite = "vehicles.write";
        /// <summary>Registrar y retirar los vehículos de las unidades propias (portal).</summary>
        public const string VehiclesWriteOwn = "vehicles.write_own";

        public const string ParkingRead = "parking.read";
        /// <summary>Solo los parqueaderos privados de las unidades propias (portal).</summary>
        public const string ParkingReadOwn = "parking.read_own";
        public const string ParkingWrite = "parking.write";
        public const string ParkingAssign = "parking.assign";
        public const string ParkingReserve = "parking.reserve";

        public const string PeopleRead = "people.read";
        public const string PeopleWrite = "people.write";
        /// <summary>Ver teléfono y correo de las personas.</summary>
        public const string PeopleViewContact = "people.view_contact";
        /// <summary>Ver el número de documento completo; sin él sale enmascarado.</summary>
        public const string PeopleViewIdentity = "people.view_identity";

        public const string UnitsRead = "units.read";
        /// <summary>Solo las unidades con las que se tiene un vínculo vigente (portal).</summary>
        public const string UnitsReadOwn = "units.read_own";
        public const string UnitsWrite = "units.write";
        /// <summary>Permite a un propietario actuar sobre unidades que no son suyas.</summary>
        public const string UnitsManageAny = "units.manage_any";

        public const string OwnershipWrite = "ownership.write";

        public const string OccupancyWrite = "occupancy.write";
        /// <summary>Registrar y finalizar ocupantes de las unidades propias (portal del propietario).</summary>
        public const string OccupancyWriteOwn = "occupancy.write_own";

        public const string UsersRead = "users.read";
        public const string UsersManage = "users.manage";

        public const string RolesRead = "roles.read";
        public const string RolesManage = "roles.manage";

        public const string AuditRead = "audit.read";

        public const string SettingsRead = "settings.read";
        public const string SettingsManage = "settings.manage";

        public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
        {
            new(DashboardRead, "Panel", "Ver el panel"),

            new(VisitsRead, "Visitas", "Consultar todas las visitas del conjunto"),
            new(VisitsReadOwn, "Visitas", "Consultar las visitas de sus propias unidades"),
            new(AccessRegister, "Portería", "Validar visitantes y registrar ingresos, salidas y rechazos"),
            new(AccessException, "Portería", "Permitir accesos excepcionales"),
            new(AccessVoid, "Portería", "Anular eventos de acceso registrados por error"),

            new(AccessRead, "Portería", "Consultar el historial de accesos del conjunto"),
            new(AccessReadOwn, "Portería", "Consultar el historial de accesos de sus propias unidades (portal)"),
            new(IncidentsWrite, "Portería", "Registrar novedades del turno"),

            new(ReportsRead, "Reportes", "Consultar reportes e indicadores del conjunto"),
            new(ReportsExport, "Reportes", "Exportar reportes e historiales a CSV"),

            new(AuthorizationsRead, "Autorizaciones", "Consultar las autorizaciones de visita del conjunto"),
            new(AuthorizationsWrite, "Autorizaciones", "Crear y revocar autorizaciones en nombre de una unidad"),
            new(AuthorizationsWriteOwn, "Autorizaciones", "Autorizar visitas a sus propias unidades (portal)"),

            new(VehiclesRead, "Vehículos", "Consultar el inventario de vehículos"),
            new(VehiclesWrite, "Vehículos", "Registrar, reasignar, suspender y revocar vehículos"),
            new(VehiclesWriteOwn, "Vehículos", "Registrar los vehículos de sus propias unidades (portal)"),

            new(ParkingRead, "Parqueaderos", "Consultar parqueaderos y su disponibilidad"),
            new(ParkingReadOwn, "Parqueaderos", "Consultar los parqueaderos de sus propias unidades (portal)"),
            new(ParkingWrite, "Parqueaderos", "Crear y editar parqueaderos, titulares y periodos fuera de servicio"),
            new(ParkingAssign, "Parqueaderos", "Asignar temporalmente y ocupar parqueaderos"),
            new(ParkingReserve, "Parqueaderos", "Reservar parqueaderos comunitarios"),

            new(PeopleRead, "Personas", "Buscar y consultar personas"),
            new(PeopleWrite, "Personas", "Crear y editar personas"),
            new(PeopleViewContact, "Personas", "Ver teléfono y correo de las personas"),
            new(PeopleViewIdentity, "Personas", "Ver el número de documento completo"),

            new(UnitsRead, "Unidades", "Consultar unidades y su ficha"),
            new(UnitsReadOwn, "Unidades", "Consultar sus propias unidades (portal)"),
            new(UnitsWrite, "Unidades", "Crear y editar estructuras y unidades"),
            new(UnitsManageAny, "Unidades", "Actuar desde el portal sobre unidades ajenas"),
            new(OwnershipWrite, "Unidades", "Registrar y cambiar propietarios"),
            new(OccupancyWrite, "Unidades", "Registrar, aprobar y finalizar ocupantes"),
            new(OccupancyWriteOwn, "Unidades", "Gestionar los ocupantes de sus propias unidades (portal)"),

            new(UsersRead, "Usuarios y roles", "Consultar usuarios"),
            new(UsersManage, "Usuarios y roles", "Crear, editar, activar y desactivar usuarios"),
            new(RolesRead, "Usuarios y roles", "Consultar roles y sus permisos"),
            new(RolesManage, "Usuarios y roles", "Crear roles y cambiar sus permisos"),

            new(AuditRead, "Bitácora", "Consultar la bitácora de cambios"),

            new(SettingsRead, "Configuración", "Consultar las políticas del conjunto"),
            new(SettingsManage, "Configuración", "Cambiar las políticas del conjunto"),
        };

        public static readonly IReadOnlySet<string> AllKeys = All.Select(p => p.Key).ToHashSet();

        public static bool IsValid(string key) => AllKeys.Contains(key);

        /// <summary>Un permiso de solo consulta termina en .read o .read_own; exportar tampoco modifica información.</summary>
        public static bool IsReadOnly(string key) => key.EndsWith(".read") || key.EndsWith(".read_own") || key.EndsWith(".export");
    }

    /// <summary>
    /// Roles con los que nace cada conjunto. Se pueden ajustar permiso por permiso, pero no borrar.
    /// Los permisos de aquí son los que reciben al crearse y cuando aparece un permiso nuevo.
    /// </summary>
    public static class SystemRoles
    {
        public const string Admin = "admin";
        public const string Guard = "guard";
        public const string Owner = "owner";
        public const string Resident = "resident";
        public const string Auditor = "auditor";

        public static readonly IReadOnlyList<SystemRoleDefinition> All = new List<SystemRoleDefinition>
        {
            new(Admin, "Administrador del conjunto",
                "Gestiona unidades, personas, visitantes, usuarios y políticas del conjunto.",
                // el administrador recibe todo, salvo las variantes \"solo lo propio\" que ya quedan cubiertas
                Permissions.AllKeys.Where(k => !k.EndsWith("_own") && k != Permissions.UnitsManageAny).ToHashSet()),

            new(Guard, "Portería",
                "Valida autorizaciones y registra ingresos y salidas.",
                Set(Permissions.DashboardRead, Permissions.VisitsRead, Permissions.AccessRegister,
                    Permissions.AuthorizationsRead, Permissions.PeopleRead, Permissions.PeopleWrite, Permissions.UnitsRead,
                    Permissions.VehiclesRead, Permissions.ParkingRead, Permissions.AccessRead, Permissions.IncidentsWrite)),

            new(Owner, "Propietario",
                "Consulta sus propiedades y las visitas a ellas.",
                Set(Permissions.DashboardRead, Permissions.VisitsReadOwn, Permissions.UnitsReadOwn, Permissions.OccupancyWriteOwn,
                    Permissions.AuthorizationsWriteOwn, Permissions.VehiclesWriteOwn, Permissions.ParkingReadOwn, Permissions.AccessReadOwn)),

            new(Resident, "Residente o arrendatario",
                "Consulta su residencia y las visitas a ella.",
                Set(Permissions.DashboardRead, Permissions.VisitsReadOwn, Permissions.UnitsReadOwn, Permissions.AuthorizationsWriteOwn,
                    Permissions.VehiclesWriteOwn, Permissions.ParkingReadOwn, Permissions.AccessReadOwn)),

            new(Auditor, "Auditor",
                "Consulta registros y reportes sin modificar información.",
                // consulta todo y puede exportar; ningún permiso de escritura
                Permissions.AllKeys.Where(k => k.EndsWith(".read") || k == Permissions.ReportsExport).ToHashSet()),
        };

        public static SystemRoleDefinition? Find(string? key) => All.FirstOrDefault(r => r.Key == key);

        private static IReadOnlySet<string> Set(params string[] keys) => new HashSet<string>(keys);
    }
}
