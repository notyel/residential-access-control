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
        public const string VisitsRegister = "visits.register";
        public const string VisitsUpdate = "visits.update";
        public const string VisitsCheckout = "visits.checkout";

        public const string PeopleRead = "people.read";
        public const string PeopleWrite = "people.write";

        public const string ResidencesRead = "residences.read";

        public const string UsersRead = "users.read";
        public const string UsersManage = "users.manage";

        public const string RolesRead = "roles.read";
        public const string RolesManage = "roles.manage";

        public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
        {
            new(DashboardRead, "Panel", "Ver el panel"),

            new(VisitsRead, "Visitas", "Consultar todas las visitas del conjunto"),
            new(VisitsReadOwn, "Visitas", "Consultar las visitas de sus propias unidades"),
            new(VisitsRegister, "Visitas", "Registrar el ingreso de una visita"),
            new(VisitsUpdate, "Visitas", "Corregir datos de una visita en curso"),
            new(VisitsCheckout, "Visitas", "Registrar la salida de una visita"),

            new(PeopleRead, "Personas", "Buscar y consultar personas"),
            new(PeopleWrite, "Personas", "Crear y editar personas"),

            new(ResidencesRead, "Unidades", "Consultar unidades"),

            new(UsersRead, "Usuarios y roles", "Consultar usuarios"),
            new(UsersManage, "Usuarios y roles", "Crear, editar, activar y desactivar usuarios"),
            new(RolesRead, "Usuarios y roles", "Consultar roles y sus permisos"),
            new(RolesManage, "Usuarios y roles", "Crear roles y cambiar sus permisos"),
        };

        public static readonly IReadOnlySet<string> AllKeys = All.Select(p => p.Key).ToHashSet();

        public static bool IsValid(string key) => AllKeys.Contains(key);

        /// <summary>Un permiso de solo consulta termina en .read o .read_own.</summary>
        public static bool IsReadOnly(string key) => key.EndsWith(".read") || key.EndsWith(".read_own");
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
                Permissions.AllKeys.Where(k => !k.EndsWith("_own")).ToHashSet()),

            new(Guard, "Portería",
                "Valida autorizaciones y registra ingresos y salidas.",
                Set(Permissions.DashboardRead, Permissions.VisitsRead, Permissions.VisitsRegister,
                    Permissions.VisitsUpdate, Permissions.VisitsCheckout, Permissions.PeopleRead,
                    Permissions.PeopleWrite, Permissions.ResidencesRead)),

            new(Owner, "Propietario",
                "Consulta sus propiedades y las visitas a ellas.",
                Set(Permissions.DashboardRead, Permissions.VisitsReadOwn)),

            new(Resident, "Residente o arrendatario",
                "Consulta su residencia y las visitas a ella.",
                Set(Permissions.DashboardRead, Permissions.VisitsReadOwn)),

            new(Auditor, "Auditor",
                "Consulta registros y reportes sin modificar información.",
                Permissions.AllKeys.Where(k => k.EndsWith(".read")).ToHashSet()),
        };

        public static SystemRoleDefinition? Find(string? key) => All.FirstOrDefault(r => r.Key == key);

        private static IReadOnlySet<string> Set(params string[] keys) => new HashSet<string>(keys);
    }
}
