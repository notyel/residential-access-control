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
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Path", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("70c1e2e6-410f-4bcd-b25f-59a2723c46c4"), new DateTime(2025, 11, 10, 16, 9, 17, 632, DateTimeKind.Utc).AddTicks(6771), true, "Dashboard", "/dashboard", null },
                    { new Guid("72cf8d54-d378-4fbf-b405-3b9aba54054f"), new DateTime(2025, 11, 10, 16, 9, 17, 632, DateTimeKind.Utc).AddTicks(6814), true, "Users", "/users", null },
                    { new Guid("e37221d9-a167-4302-8af9-ae9454e5139d"), new DateTime(2025, 11, 10, 16, 9, 17, 632, DateTimeKind.Utc).AddTicks(6786), true, "Visits", "/visits", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("003e0a7f-f42d-4a41-bd7a-3951dcc2c5ca"), "001", new DateTime(2025, 11, 10, 16, 9, 17, 228, DateTimeKind.Utc).AddTicks(7255), "admin@accesscontrol.com", "Admin", true, "User", "$2a$11$dJXYBidufZNnTvlRGDsChuaVzI7tqrmBkfd16nAaWXXO7F9snPizG", 0, null },
                    { new Guid("64a11d2f-3b49-4c68-a6b7-be16ed0b5f0c"), "101", new DateTime(2025, 11, 10, 16, 9, 17, 432, DateTimeKind.Utc).AddTicks(1847), "owner@accesscontrol.com", "John", true, "Doe", "$2a$11$qhul2mDDa.bP/iE/4P.pzuSaxXBy3yG8PKO8MgKTwDDUrqGmQqX4W", 2, null },
                    { new Guid("a5e0389f-38a2-4e0a-91f2-3ad0fdb78c12"), "002", new DateTime(2025, 11, 10, 16, 9, 17, 632, DateTimeKind.Utc).AddTicks(5190), "guard@accesscontrol.com", "Guard", true, "User", "$2a$11$uMckiZ1YRxLqisKsh82N2eX/OjOd9loD/6n4e33VT8YC48S4WkvEu", 1, null }
                });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "OwnerId", "UpdatedAt" },
                values: new object[] { new Guid("0d98e5eb-7a34-4953-95cd-5ac0bb558f5e"), new DateTime(2025, 11, 10, 16, 9, 17, 632, DateTimeKind.Utc).AddTicks(6611), "RES-001", true, new Guid("64a11d2f-3b49-4c68-a6b7-be16ed0b5f0c"), null });

            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: new[] { "Id", "MenuId", "Role" },
                values: new object[,]
                {
                    { new Guid("24e0ece9-1388-4919-8573-9d683561a3a8"), new Guid("e37221d9-a167-4302-8af9-ae9454e5139d"), 0 },
                    { new Guid("69a69007-a723-4bf3-aed0-db9facfae7df"), new Guid("72cf8d54-d378-4fbf-b405-3b9aba54054f"), 0 },
                    { new Guid("918e0c20-692c-4727-bc06-95e492901b3b"), new Guid("70c1e2e6-410f-4bcd-b25f-59a2723c46c4"), 0 },
                    { new Guid("a5dece41-cfc3-496a-82f1-fa8e0a544a31"), new Guid("e37221d9-a167-4302-8af9-ae9454e5139d"), 2 },
                    { new Guid("b75b15d5-75bb-4c2c-9f6c-e048282b833f"), new Guid("e37221d9-a167-4302-8af9-ae9454e5139d"), 1 },
                    { new Guid("cc22db92-d9aa-40a6-8cf2-238252f52539"), new Guid("70c1e2e6-410f-4bcd-b25f-59a2723c46c4"), 2 },
                    { new Guid("eec202fc-ea5b-4d42-8545-7564e4123bc0"), new Guid("70c1e2e6-410f-4bcd-b25f-59a2723c46c4"), 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("0d98e5eb-7a34-4953-95cd-5ac0bb558f5e"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("24e0ece9-1388-4919-8573-9d683561a3a8"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("69a69007-a723-4bf3-aed0-db9facfae7df"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("918e0c20-692c-4727-bc06-95e492901b3b"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("a5dece41-cfc3-496a-82f1-fa8e0a544a31"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("b75b15d5-75bb-4c2c-9f6c-e048282b833f"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("cc22db92-d9aa-40a6-8cf2-238252f52539"));

            migrationBuilder.DeleteData(
                table: "RoleMenus",
                keyColumn: "Id",
                keyValue: new Guid("eec202fc-ea5b-4d42-8545-7564e4123bc0"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("003e0a7f-f42d-4a41-bd7a-3951dcc2c5ca"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a5e0389f-38a2-4e0a-91f2-3ad0fdb78c12"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("70c1e2e6-410f-4bcd-b25f-59a2723c46c4"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("72cf8d54-d378-4fbf-b405-3b9aba54054f"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("e37221d9-a167-4302-8af9-ae9454e5139d"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("64a11d2f-3b49-4c68-a6b7-be16ed0b5f0c"));
        }
    }
}
