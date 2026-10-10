using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeterministicSeedAndMenuPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddOrderToMenu insertó a mano un menú "Residentes" que ninguna migración posterior borró;
            // choca con el índice único de Name. Los menús son datos sembrados: se dejan solo los tres vigentes.
            migrationBuilder.Sql(@"
                DELETE FROM ""Menus"" WHERE ""Id"" NOT IN (
                    '9452df48-5b92-40ef-829e-a315c01aadcb',
                    'a509d9b3-9c77-4324-bc20-4a004b8c5abf',
                    'd8fcac7d-c0ea-4cd5-8ad0-92b33953a142');");

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Panel", "/access-control/dashboard" });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Visitas", "/access-control/visits" });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Residentes", "/access-control/residents" });

            migrationBuilder.UpdateData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("8c885dcd-3734-45b6-9b7e-633351852714"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1924), "Dashboard", "/dashboard" });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1941), "Visits", "/visits" });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                columns: new[] { "CreatedAt", "Name", "Path" },
                values: new object[] { new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1962), "Users", "/users" });

            migrationBuilder.UpdateData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("8c885dcd-3734-45b6-9b7e-633351852714"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(327));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 23, 47, 38, 434, DateTimeKind.Utc).AddTicks(4783));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 21, 23, 47, 38, 641, DateTimeKind.Utc).AddTicks(7232));
        }
    }
}
