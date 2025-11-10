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
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("18739275-5cdd-423f-9cab-ab6eb971f05f"), "101", new DateTime(2025, 11, 10, 15, 59, 42, 576, DateTimeKind.Utc).AddTicks(4974), "owner@accesscontrol.com", "John", true, "Doe", "$2a$11$EjuLD0h2HCCjf2R/IUAjTeVk6aHxpORjJRRj2z7taVQ/aYFVZ8z4S", 2, null },
                    { new Guid("b4d6e89d-0062-46df-a812-7f8cafc127e6"), "002", new DateTime(2025, 11, 10, 15, 59, 42, 777, DateTimeKind.Utc).AddTicks(654), "guard@accesscontrol.com", "Guard", true, "User", "$2a$11$9b0pzdUx/FI5dqjE2bn62OFRdinUrMiiEe2tZehSntty3PbqbMOd6", 1, null },
                    { new Guid("c05a5096-b169-404a-94d0-e896627d37ff"), "001", new DateTime(2025, 11, 10, 15, 59, 42, 370, DateTimeKind.Utc).AddTicks(2925), "admin@accesscontrol.com", "Admin", true, "User", "$2a$11$9/VyK3rzxBjmlcPGKVdBH.rQaRjSUggzdSWM6UTKBH9xCn.cMcyxC", 0, null }
                });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "CreatedAt", "Identifier", "IsActive", "OwnerId", "UpdatedAt" },
                values: new object[] { new Guid("090aebe9-5996-49a8-88da-c62bfc70ce3d"), new DateTime(2025, 11, 10, 15, 59, 42, 777, DateTimeKind.Utc).AddTicks(1945), "RES-001", true, new Guid("18739275-5cdd-423f-9cab-ab6eb971f05f"), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("090aebe9-5996-49a8-88da-c62bfc70ce3d"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b4d6e89d-0062-46df-a812-7f8cafc127e6"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("c05a5096-b169-404a-94d0-e896627d37ff"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("18739275-5cdd-423f-9cab-ab6eb971f05f"));
        }
    }
}
