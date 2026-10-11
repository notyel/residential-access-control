using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncidentsAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehiclePlate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incidents_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("5f6a7b8c-9dae-4fb0-91c2-4d5e6f708192"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Control", "History", true, "Historial de accesos", 21, "/access-control/access-history", "access.read,access.read_own", null, null },
                    { new Guid("6a7b8c9d-aebf-40c1-a2d3-5e6f708192a3"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Control", "ClipboardList", true, "Novedades", 20, "/access-control/incidents", "incidents.write,access.read", null, null },
                    { new Guid("7b8c9dae-bfc0-41d2-b3e4-6f708192a3b4"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Control", "ChartColumn", true, "Reportes", 22, "/access-control/reports", "reports.read", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_AuthorId",
                table: "Incidents",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ComplexId_OccurredAt",
                table: "Incidents",
                columns: new[] { "ComplexId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_PersonId",
                table: "Incidents",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_UnitId",
                table: "Incidents",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("5f6a7b8c-9dae-4fb0-91c2-4d5e6f708192"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("6a7b8c9d-aebf-40c1-a2d3-5e6f708192a3"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("7b8c9dae-bfc0-41d2-b3e4-6f708192a3b4"));
        }
    }
}
