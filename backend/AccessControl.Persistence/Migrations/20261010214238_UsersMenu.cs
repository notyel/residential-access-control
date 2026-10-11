using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsersMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                column: "Group",
                value: null);

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt" },
                values: new object[] { new Guid("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Usuarios y roles", "UserCog", true, "Usuarios", 4, "/access-control/users", "users.read", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809"));

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                column: "Group",
                value: "Usuarios y roles");
        }
    }
}
