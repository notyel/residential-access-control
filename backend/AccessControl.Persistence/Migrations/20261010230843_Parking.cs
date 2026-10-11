using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Parking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    VehicleKind = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSpaces_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParkingAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Class = table.Column<int>(type: "integer", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsException = table.Column<bool>(type: "boolean", nullable: false),
                    ExceptionReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingAssignments", x => x.Id);
                    table.CheckConstraint("CK_ParkingAssignments_Dates", "\"EndsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\"");
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_ParkingSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "ParkingSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingAssignments_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParkingNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingNotes_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingNotes_ParkingSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "ParkingSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParkingOutOfService",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    From = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    To = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingOutOfService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingOutOfService_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingOutOfService_ParkingSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "ParkingSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("3d4e5f6a-7b8c-4d9e-bfa0-2b3c4d5e6f70"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "SquareParking", true, "Parqueaderos", 9, "/access-control/parking", "parking.read", null, null },
                    { new Guid("4e5f6a7b-8c9d-4eaf-80b1-3c4d5e6f7081"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "SquareParking", true, "Mis parqueaderos", 14, "/access-control/my/parking", "parking.read_own", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_ComplexId",
                table: "ParkingAssignments",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_PersonId",
                table: "ParkingAssignments",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_SpaceId_Status_StartsAt",
                table: "ParkingAssignments",
                columns: new[] { "SpaceId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_UnitId",
                table: "ParkingAssignments",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_VehicleId_Status",
                table: "ParkingAssignments",
                columns: new[] { "VehicleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingAssignments_VisitId",
                table: "ParkingAssignments",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingNotes_AuthorId",
                table: "ParkingNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingNotes_ComplexId",
                table: "ParkingNotes",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingNotes_SpaceId_CreatedAt",
                table: "ParkingNotes",
                columns: new[] { "SpaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingOutOfService_ComplexId",
                table: "ParkingOutOfService",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingOutOfService_SpaceId_From",
                table: "ParkingOutOfService",
                columns: new[] { "SpaceId", "From" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpaces_ComplexId_Code",
                table: "ParkingSpaces",
                columns: new[] { "ComplexId", "Code" },
                unique: true);

            // ---- La base impide asignaciones incompatibles: dos peticiones simultáneas no pueden ganar ambas ----
            // Clases: 0 titular, 1 temporal, 2 reserva, 3 ocupación. Solo cuentan las vigentes (Status = 0).
            // btree_gist ya se instaló con las unidades (permite mezclar = y && en la misma restricción).
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                -- titular, asignación temporal y reserva se excluyen entre sí en el mismo espacio
                ALTER TABLE ""ParkingAssignments"" ADD CONSTRAINT ""EX_ParkingAssignments_Planned""
                    EXCLUDE USING gist (""SpaceId"" WITH =, tstzrange(""StartsAt"", ""EndsAt"", '[)') WITH &&)
                    WHERE (""Status"" = 0 AND ""Class"" IN (0, 1, 2));

                -- un espacio, un vehículo a la vez (la ocupación sí convive con el titular del privado)
                ALTER TABLE ""ParkingAssignments"" ADD CONSTRAINT ""EX_ParkingAssignments_Occupancy""
                    EXCLUDE USING gist (""SpaceId"" WITH =, tstzrange(""StartsAt"", ""EndsAt"", '[)') WITH &&)
                    WHERE (""Status"" = 0 AND ""Class"" = 3);

                -- un vehículo no ocupa dos espacios a la vez
                ALTER TABLE ""ParkingAssignments"" ADD CONSTRAINT ""EX_ParkingAssignments_Vehicle""
                    EXCLUDE USING gist (""VehicleId"" WITH =, tstzrange(""StartsAt"", ""EndsAt"", '[)') WITH &&)
                    WHERE (""Status"" = 0 AND ""Class"" = 3 AND ""VehicleId"" IS NOT NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingAssignments");

            migrationBuilder.DropTable(
                name: "ParkingNotes");

            migrationBuilder.DropTable(
                name: "ParkingOutOfService");

            migrationBuilder.DropTable(
                name: "ParkingSpaces");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("3d4e5f6a-7b8c-4d9e-bfa0-2b3c4d5e6f70"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("4e5f6a7b-8c9d-4eaf-80b1-3c4d5e6f7081"));
        }
    }
}
