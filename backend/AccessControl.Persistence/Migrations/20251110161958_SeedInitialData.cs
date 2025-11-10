using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Menus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsActive", "Name", "Path", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("b7dabed6-e654-4ba6-9f50-a0dc2f24e9ac"), new DateTime(2025, 11, 10, 16, 19, 57, 712, DateTimeKind.Utc).AddTicks(4786), "Users", true, "Users", "/users", null },
                    { new Guid("e4109e58-ac11-4cb9-a538-710ee4b07afe"), new DateTime(2025, 11, 10, 16, 19, 57, 712, DateTimeKind.Utc).AddTicks(4757), "CarFront", true, "Visits", "/visits", null },
                    { new Guid("fd62efee-c22c-432d-9b6b-1ed0cceea663"), new DateTime(2025, 11, 10, 16, 19, 57, 712, DateTimeKind.Utc).AddTicks(4553), "LayoutDashboard", true, "Dashboard", "/dashboard", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("564ea613-4aa7-48bb-9fcc-c97bfe77122b"), "002", new DateTime(2025, 11, 10, 16, 19, 57, 712, DateTimeKind.Utc).AddTicks(2800), "guard@accesscontrol.com", "Guard", true, "User", "$2a$11$4HxulFqrdJSL40CC9c0PEuKTHzUjxCIKE9c95JCQOmTHDhphaYv.i", 1, null },
                    { new Guid("5afe1bf5-2e00-4e38-a0d9-0e80c8418303"), "101", new DateTime(2025, 11, 10, 16, 19, 57, 510, DateTimeKind.Utc).AddTicks(7210), "owner@accesscontrol.com", "John", true, "Doe", "$2a$11$baw5u69n3UpBglq4A2k2ZeO3X1qk6NSgjXbdBvXjUAenqiIBsKCLq", 2, null },
                    { new Guid("c4daa537-604b-48f8-880a-9a49b0ab5d56"), "001", new DateTime(2025, 11, 10, 16, 19, 57, 308, DateTimeKind.Utc).AddTicks(4282), "admin@accesscontrol.com", "Admin", true, "User", "$2a$11$h5VJArtUEPEf.a4mzXU.wuPBuxT0XLCS2D.mlEPTZBn99f3Ar0vaa", 0, null }
                });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "OwnerId", "UpdatedAt" },
                values: new object[] { new Guid("a0dfde6e-ebda-4809-864d-3c7ee8d5989e"), new DateTime(2025, 11, 10, 16, 19, 57, 712, DateTimeKind.Utc).AddTicks(4384), "RES-001", true, new Guid("5afe1bf5-2e00-4e38-a0d9-0e80c8418303"), null });

            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: new[] { "Id", "MenuId", "Role" },
                values: new object[,]
                {
                    { new Guid("01c2109a-a25d-4d59-98bc-5770224198a8"), new Guid("e4109e58-ac11-4cb9-a538-710ee4b07afe"), 0 },
                    { new Guid("09647dd5-1574-45a7-a969-d7a38859e3b8"), new Guid("b7dabed6-e654-4ba6-9f50-a0dc2f24e9ac"), 0 },
                    { new Guid("117fa697-5f7c-4930-8a47-707c1b9adef8"), new Guid("fd62efee-c22c-432d-9b6b-1ed0cceea663"), 1 },
                    { new Guid("364a06eb-de3b-48b2-82f4-519d94a511e6"), new Guid("fd62efee-c22c-432d-9b6b-1ed0cceea663"), 2 },
                    { new Guid("58dc24c9-683a-463e-a4a3-2f7bd4ee9e15"), new Guid("e4109e58-ac11-4cb9-a538-710ee4b07afe"), 2 },
                    { new Guid("5dc4fb40-b77f-4c73-8291-9a65104898cf"), new Guid("fd62efee-c22c-432d-9b6b-1ed0cceea663"), 0 },
                    { new Guid("bb56a956-9acd-4188-a1a1-b88917559b55"), new Guid("e4109e58-ac11-4cb9-a538-710ee4b07afe"), 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("a0dfde6e-ebda-4809-864d-3c7ee8d5989e"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("01c2109a-a25d-4d59-98bc-5770224198a8"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("09647dd5-1574-45a7-a969-d7a38859e3b8"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("117fa697-5f7c-4930-8a47-707c1b9adef8"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("364a06eb-de3b-48b2-82f4-519d94a511e6"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("58dc24c9-683a-463e-a4a3-2f7bd4ee9e15"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("5dc4fb40-b77f-4c73-8291-9a65104898cf"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("bb56a956-9acd-4188-a1a1-b88917559b55"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("564ea613-4aa7-48bb-9fcc-c97bfe77122b"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("c4daa537-604b-48f8-880a-9a49b0ab5d56"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7dabed6-e654-4ba6-9f50-a0dc2f24e9ac"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("e4109e58-ac11-4cb9-a538-710ee4b07afe"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("fd62efee-c22c-432d-9b6b-1ed0cceea663"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5afe1bf5-2e00-4e38-a0d9-0e80c8418303"));

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Menus");
        }
    }
}
