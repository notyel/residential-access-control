using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComplexPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplexPolicies",
                columns: table => new
                {
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexPolicies", x => new { x.ComplexId, x.Key });
                    table.ForeignKey(
                        name: "FK_ComplexPolicies_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[] { new Guid("f6c3e5a7-8d91-42a3-b4c5-d6e7f8091a2b"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Configuración", "Settings", true, "Políticas", 95, "/access-control/settings/policies", "settings.read", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplexPolicies");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("f6c3e5a7-8d91-42a3-b4c5-d6e7f8091a2b"));
        }
    }
}
