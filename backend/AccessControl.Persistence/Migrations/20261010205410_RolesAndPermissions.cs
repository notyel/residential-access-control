using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMenus");

            migrationBuilder.AddColumn<string>(
                name: "Permission",
                table: "Menus",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppliedPermissionDefaults",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedPermissionDefaults", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SystemKey = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.Permission });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"),
                column: "Permission",
                value: "dashboard.read");

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                column: "Permission",
                value: "visits.read,visits.read_own");

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                column: "Permission",
                value: "users.read");

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt" },
                values: new object[] { new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "ShieldCheck", true, "Roles", 4, "/access-control/roles", "roles.read", null });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ComplexId_Name",
                table: "Roles",
                columns: new[] { "ComplexId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            // Roles de sistema para cada conjunto existente. Sus permisos los reparte RoleProvisioner al
            // arrancar la API (aún no hay ninguno marcado como repartido).
            migrationBuilder.Sql(@"
                INSERT INTO ""Roles"" (""Id"", ""ComplexId"", ""Name"", ""Description"", ""SystemKey"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), c.""Id"", r.name, r.description, r.key, now(), true
                FROM ""Complexes"" c
                CROSS JOIN (VALUES
                    ('admin', 'Administrador del conjunto', 'Gestiona unidades, personas, visitantes, usuarios y políticas del conjunto.'),
                    ('guard', 'Portería', 'Valida autorizaciones y registra ingresos y salidas.'),
                    ('owner', 'Propietario', 'Consulta sus propiedades y las visitas a ellas.'),
                    ('resident', 'Residente o arrendatario', 'Consulta su residencia y las visitas a ella.'),
                    ('auditor', 'Auditor', 'Consulta registros y reportes sin modificar información.')
                ) AS r(key, name, description);");

            // Cada usuario conserva lo que podía hacer: su rol fijo anterior (0 Admin, 1 Guard, 2 Owner)
            // pasa a ser el rol de sistema equivalente de su conjunto. El superadministrador no lleva rol.
            migrationBuilder.Sql(@"
                INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"")
                SELECT u.""Id"", r.""Id""
                FROM ""Users"" u
                JOIN (VALUES (0, 'admin'), (1, 'guard'), (2, 'owner')) AS m(old_role, key) ON m.old_role = u.""Role""
                JOIN ""Roles"" r ON r.""ComplexId"" = u.""ComplexId"" AND r.""SystemKey"" = m.key
                WHERE u.""IsSuperAdmin"" = false;");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppliedPermissionDefaults");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"));

            migrationBuilder.DropColumn(
                name: "Permission",
                table: "Menus");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoleMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleMenus_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: new[] { "Id", "MenuId", "Role" },
                values: new object[,]
                {
                    { new Guid("8c1587ae-f51b-48c6-97d6-04f31b3c057c"), new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"), 1 },
                    { new Guid("9b05f72c-eadb-49ec-84bf-f895d549f5cc"), new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"), 2 },
                    { new Guid("9ea9bd6d-11ee-4d47-acaa-32fce37c34d0"), new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"), 0 },
                    { new Guid("bc524814-0d6b-4ff7-a227-2fa9c1ad589e"), new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"), 2 },
                    { new Guid("c70ee399-2739-4d76-a6a5-b4848f7273e3"), new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"), 0 },
                    { new Guid("d2370f28-6288-4482-a93c-7849648a5991"), new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"), 1 },
                    { new Guid("e650bfce-65ef-416e-8f10-f56fa43a6edc"), new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"), 0 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                column: "Role",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f"),
                column: "Role",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                column: "Role",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                column: "Role",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMenus_MenuId",
                table: "RoleMenus",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMenus_Role_MenuId",
                table: "RoleMenus",
                columns: new[] { "Role", "MenuId" },
                unique: true);
        }
    }
}
