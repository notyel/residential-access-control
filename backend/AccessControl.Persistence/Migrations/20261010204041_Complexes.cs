using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Complexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Residences_Identifier",
                table: "Residences");

            migrationBuilder.DropIndex(
                name: "IX_Persons_DocumentNumber",
                table: "Persons");

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexId",
                table: "Visits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexId",
                table: "Residences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexId",
                table: "Persons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Complexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complexes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Complexes",
                columns: new[] { "Id", "Address", "CreatedAt", "IsActive", "Name", "TimeZoneId", "Type", "UpdatedAt" },
                values: new object[] { new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"), null, new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), true, "Conjunto principal", "America/Bogota", 0, null });

            // Los datos que ya existían pasan al conjunto inicial (los usuarios aún no incluyen al superadministrador)
            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""ComplexId"" = '0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f' WHERE ""ComplexId"" IS NULL;
                UPDATE ""Residences"" SET ""ComplexId"" = '0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f';
                UPDATE ""Persons"" SET ""ComplexId"" = '0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f';
                UPDATE ""Visits"" SET ""ComplexId"" = '0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f';");

            migrationBuilder.UpdateData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("8c885dcd-3734-45b6-9b7e-633351852714"),
                column: "ComplexId",
                value: new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                columns: new[] { "ComplexId", "IsSuperAdmin" },
                values: new object[] { new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"), false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                columns: new[] { "ComplexId", "IsSuperAdmin" },
                values: new object[] { new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"), false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                columns: new[] { "ComplexId", "IsSuperAdmin" },
                values: new object[] { new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"), false });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApartmentNumber", "ComplexId", "CreatedAt", "Email", "FirstName", "IsActive", "IsSuperAdmin", "LastName", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { new Guid("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f"), "", null, new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), "superadmin@accesscontrol.com", "Super", true, true, "Admin", "$2a$11$NrwVmJEOdhpPYxIjwzhi8e79QrYbaW25WmXZXTOPwq70vuN4Rpl/S", 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ComplexId_CreatedAt",
                table: "Visits",
                columns: new[] { "ComplexId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ComplexId",
                table: "Users",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_Residences_ComplexId_Identifier",
                table: "Residences",
                columns: new[] { "ComplexId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_ComplexId_DocumentNumber",
                table: "Persons",
                columns: new[] { "ComplexId", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complexes_Name",
                table: "Complexes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Complexes_ComplexId",
                table: "Persons",
                column: "ComplexId",
                principalTable: "Complexes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Residences_Complexes_ComplexId",
                table: "Residences",
                column: "ComplexId",
                principalTable: "Complexes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Complexes_ComplexId",
                table: "Users",
                column: "ComplexId",
                principalTable: "Complexes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Complexes_ComplexId",
                table: "Visits",
                column: "ComplexId",
                principalTable: "Complexes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Complexes_ComplexId",
                table: "Persons");

            migrationBuilder.DropForeignKey(
                name: "FK_Residences_Complexes_ComplexId",
                table: "Residences");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Complexes_ComplexId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Complexes_ComplexId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "Complexes");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ComplexId_CreatedAt",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Users_ComplexId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Residences_ComplexId_Identifier",
                table: "Residences");

            migrationBuilder.DropIndex(
                name: "IX_Persons_ComplexId_DocumentNumber",
                table: "Persons");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f"));

            migrationBuilder.DropColumn(
                name: "ComplexId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ComplexId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ComplexId",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "ComplexId",
                table: "Persons");

            migrationBuilder.CreateIndex(
                name: "IX_Residences_Identifier",
                table: "Residences",
                column: "Identifier");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_DocumentNumber",
                table: "Persons",
                column: "DocumentNumber",
                unique: true);
        }
    }
}
