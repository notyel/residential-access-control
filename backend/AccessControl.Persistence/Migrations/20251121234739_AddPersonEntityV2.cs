using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonEntityV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "VisitorId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "VisitorName",
                table: "Visits");

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Visits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    DocumentNumber = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    PersonType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsActive", "Name", "Order", "Path", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"), new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1924), "LayoutDashboard", true, "Dashboard", 1, "/dashboard", null },
                    { new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"), new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1941), "CarFront", true, "Visits", 2, "/visits", null },
                    { new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"), new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1962), "Users", true, "Users", 3, "/users", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"), "002", new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(327), "guard@accesscontrol.com", "Guard", true, "User", "$2a$11$bKkXvLrTLRP4MzUnYuL6Zu1S9g7g.dUolC/DcZxHqApI9YMZco45u", 1, null },
                    { new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"), "001", new DateTime(2025, 11, 21, 23, 47, 38, 434, DateTimeKind.Utc).AddTicks(4783), "admin@accesscontrol.com", "Admin", true, "User", "$2a$11$NrwVmJEOdhpPYxIjwzhi8e79QrYbaW25WmXZXTOPwq70vuN4Rpl/S", 0, null },
                    { new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"), "101", new DateTime(2025, 11, 21, 23, 47, 38, 641, DateTimeKind.Utc).AddTicks(7232), "owner@accesscontrol.com", "John", true, "Doe", "$2a$11$ftyFUyavOWRp2SriTa84WuU8QeoAjQV/9OhC1J3QzojVsjITw/osi", 2, null }
                });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "OwnerId", "UpdatedAt" },
                values: new object[] { new Guid("8c885dcd-3734-45b6-9b7e-633351852714"), new DateTime(2025, 11, 21, 23, 47, 38, 845, DateTimeKind.Utc).AddTicks(1777), "RES-001", true, new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"), null });

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

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PersonId",
                table: "Visits",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_DocumentNumber",
                table: "Persons",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Visits_PersonId",
                table: "Visits");

            migrationBuilder.DeleteData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("8c885dcd-3734-45b6-9b7e-633351852714"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("8c1587ae-f51b-48c6-97d6-04f31b3c057c"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("9b05f72c-eadb-49ec-84bf-f895d549f5cc"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("9ea9bd6d-11ee-4d47-acaa-32fce37c34d0"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("bc524814-0d6b-4ff7-a227-2fa9c1ad589e"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("c70ee399-2739-4d76-a6a5-b4848f7273e3"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("d2370f28-6288-4482-a93c-7849648a5991"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("e650bfce-65ef-416e-8f10-f56fa43a6edc"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"));

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Visits");

            migrationBuilder.AddColumn<string>(
                name: "VisitorId",
                table: "Visits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitorName",
                table: "Visits",
                type: "text",
                nullable: false,
                defaultValue: "");

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
    }
}
