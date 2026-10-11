using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ControlMenuOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("5f6a7b8c-9dae-4fb0-91c2-4d5e6f708192"),
                column: "Order",
                value: 87);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("6a7b8c9d-aebf-40c1-a2d3-5e6f708192a3"),
                column: "Order",
                value: 86);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("7b8c9dae-bfc0-41d2-b3e4-6f708192a3b4"),
                column: "Order",
                value: 88);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("5f6a7b8c-9dae-4fb0-91c2-4d5e6f708192"),
                column: "Order",
                value: 21);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("6a7b8c9d-aebf-40c1-a2d3-5e6f708192a3"),
                column: "Order",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("7b8c9dae-bfc0-41d2-b3e4-6f708192a3b4"),
                column: "Order",
                value: 22);
        }
    }
}
