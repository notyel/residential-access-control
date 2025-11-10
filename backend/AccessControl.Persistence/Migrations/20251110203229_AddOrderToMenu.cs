using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderToMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Menus",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsActive", "Name", "Order", "Path", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("239d0797-0293-4387-8e0f-3f94cb66f732"), new DateTime(2025, 11, 10, 20, 32, 29, 45, DateTimeKind.Utc).AddTicks(7769), "Users", true, "Users", 3, "/users", null },
                    { new Guid("884af528-cae0-4da4-9c6c-443020d1c6d3"), new DateTime(2025, 11, 10, 20, 32, 29, 45, DateTimeKind.Utc).AddTicks(7725), "LayoutDashboard", true, "Dashboard", 1, "/dashboard", null },
                    { new Guid("a6ba19e3-61a5-4e50-a2b6-566a8e6f8af0"), new DateTime(2025, 11, 10, 20, 32, 29, 45, DateTimeKind.Utc).AddTicks(7740), "CarFront", true, "Visits", 2, "/visits", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5d60b48f-f0c2-4f95-918d-5c5c76f89eff"), "002", new DateTime(2025, 11, 10, 20, 32, 29, 45, DateTimeKind.Utc).AddTicks(6299), "guard@accesscontrol.com", "Guard", true, "User", "$2a$11$6cX3ymDNWZ.EtQk4ulMOPubkwXex7d6ZF2QmeHNitT3hWE6R3xY6q", 1, null },
                    { new Guid("628de891-c810-469f-aadc-adfccb6df6a0"), "101", new DateTime(2025, 11, 10, 20, 32, 28, 845, DateTimeKind.Utc).AddTicks(9618), "owner@accesscontrol.com", "John", true, "Doe", "$2a$11$gbsnApsyPF1tOTZeoc6qEuly0HZiLtdbpnLgEoBd40YEhy7ul1i5O", 2, null },
                    { new Guid("eaa6a665-7a14-447b-a7df-aa83c6ba96b6"), "001", new DateTime(2025, 11, 10, 20, 32, 28, 639, DateTimeKind.Utc).AddTicks(9255), "admin@accesscontrol.com", "Admin", true, "User", "$2a$11$tYY2TwNWa.JdcBT8tuSb/Oc5EogV0vbq0a7A4Il7wtmduMJw2YhcO", 0, null }
                });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "OwnerId", "UpdatedAt" },
                values: new object[] { new Guid("3d06fa45-a3a9-4f21-9910-b857fc0b0827"), new DateTime(2025, 11, 10, 20, 32, 29, 45, DateTimeKind.Utc).AddTicks(7571), "RES-001", true, new Guid("628de891-c810-469f-aadc-adfccb6df6a0"), null });

            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: new[] { "Id", "MenuId", "Role" },
                values: new object[,]
                {
                    { new Guid("026d9c28-a8cf-4ed2-a02c-a4cab699734a"), new Guid("a6ba19e3-61a5-4e50-a2b6-566a8e6f8af0"), 0 },
                    { new Guid("1789395f-3112-4e0b-b67e-4daf9548cfbe"), new Guid("884af528-cae0-4da4-9c6c-443020d1c6d3"), 0 },
                    { new Guid("4b90d743-15d4-4443-9f96-33aa9917768c"), new Guid("a6ba19e3-61a5-4e50-a2b6-566a8e6f8af0"), 2 },
                    { new Guid("72b0373f-6fd7-4ac2-9640-cc78bddaa087"), new Guid("884af528-cae0-4da4-9c6c-443020d1c6d3"), 1 },
                    { new Guid("7674abc5-0541-4469-b776-cdf1a0b5482c"), new Guid("a6ba19e3-61a5-4e50-a2b6-566a8e6f8af0"), 1 },
                    { new Guid("81ff1a4d-919a-4aed-9136-961598d376d2"), new Guid("239d0797-0293-4387-8e0f-3f94cb66f732"), 0 },
                    { new Guid("d656dbfd-7706-4eec-9b99-9a4f1c6a5544"), new Guid("884af528-cae0-4da4-9c6c-443020d1c6d3"), 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("3d06fa45-a3a9-4f21-9910-b857fc0b0827"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("026d9c28-a8cf-4ed2-a02c-a4cab699734a"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("1789395f-3112-4e0b-b67e-4daf9548cfbe"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("4b90d743-15d4-4443-9f96-33aa9917768c"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("72b0373f-6fd7-4ac2-9640-cc78bddaa087"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("7674abc5-0541-4469-b776-cdf1a0b5482c"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("81ff1a4d-919a-4aed-9136-961598d376d2"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("d656dbfd-7706-4eec-9b99-9a4f1c6a5544"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5d60b48f-f0c2-4f95-918d-5c5c76f89eff"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("eaa6a665-7a14-447b-a7df-aa83c6ba96b6"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("239d0797-0293-4387-8e0f-3f94cb66f732"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("884af528-cae0-4da4-9c6c-443020d1c6d3"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a6ba19e3-61a5-4e50-a2b6-566a8e6f8af0"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("628de891-c810-469f-aadc-adfccb6df6a0"));

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Menus");

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
    }
}
