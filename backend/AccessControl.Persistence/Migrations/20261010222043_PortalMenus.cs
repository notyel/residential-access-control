using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortalMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("b8e5a7c9-afb3-44c5-96e7-f8091a2b3c4d"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "KeyRound", true, "Mis propiedades", 10, "/access-control/my/units", "occupancy.write_own", null, null },
                    { new Guid("c9f6b8da-b0c4-45d6-a7f8-091a2b3c4d5e"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "House", true, "Mi residencia", 11, "/access-control/my/home", "units.read_own", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b8e5a7c9-afb3-44c5-96e7-f8091a2b3c4d"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("c9f6b8da-b0c4-45d6-a7f8-091a2b3c4d5e"));
        }
    }
}
