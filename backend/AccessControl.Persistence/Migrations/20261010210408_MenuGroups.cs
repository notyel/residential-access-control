using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Menus",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"),
                column: "Group",
                value: null);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                column: "Group",
                value: null);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                column: "Group",
                value: "Usuarios y roles");

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                column: "Group",
                value: "Usuarios y roles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "Menus");
        }
    }
}
