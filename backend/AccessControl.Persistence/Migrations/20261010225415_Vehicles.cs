using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Vehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    HasPlate = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Class = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsiblePersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAuthorizations", x => x.Id);
                    table.CheckConstraint("CK_VehicleAuthorizations_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_VehicleAuthorizations_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAuthorizations_Persons_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAuthorizations_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAuthorizations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleNotes_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleNotes_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Car", true, "Vehículos", 8, "/access-control/vehicles", "vehicles.read", null, null },
                    { new Guid("1b2c3d4e-5f6a-4b7c-9d8e-0f1a2b3c4d5e"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ScanSearch", true, "Verificar vehículos", 7, "/access-control/gate/vehicles", "access.register", null, null },
                    { new Guid("2c3d4e5f-6a7b-4c8d-ae9f-1a2b3c4d5e6f"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Car", true, "Mis vehículos", 13, "/access-control/my/vehicles", "vehicles.write_own", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VehicleId",
                table: "Visits",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_VehicleId_OccurredAt",
                table: "AccessEvents",
                columns: new[] { "VehicleId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAuthorizations_ComplexId",
                table: "VehicleAuthorizations",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAuthorizations_ResponsiblePersonId",
                table: "VehicleAuthorizations",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAuthorizations_UnitId_Status",
                table: "VehicleAuthorizations",
                columns: new[] { "UnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_VehicleAuthorizations_Open",
                table: "VehicleAuthorizations",
                column: "VehicleId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleNotes_AuthorId",
                table: "VehicleNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleNotes_ComplexId",
                table: "VehicleNotes",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleNotes_VehicleId_CreatedAt",
                table: "VehicleNotes",
                columns: new[] { "VehicleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ComplexId_Plate",
                table: "Vehicles",
                columns: new[] { "ComplexId", "Plate" },
                unique: true);

            // ---- Las placas de las visitas anteriores pasan a ser vehículos visitantes ----
            migrationBuilder.Sql(@"
                INSERT INTO ""Vehicles"" (""Id"", ""ComplexId"", ""Plate"", ""HasPlate"", ""Type"", ""Class"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), p.""ComplexId"", p.""Plate"", true, 'automovil', 1, now(), true
                FROM (SELECT DISTINCT ""ComplexId"", upper(regexp_replace(""VehiclePlate"", '[^A-Za-z0-9]', '', 'g')) AS ""Plate""
                      FROM ""Visits"" WHERE ""VehiclePlate"" IS NOT NULL) p
                WHERE p.""Plate"" <> '';

                UPDATE ""Visits"" v SET ""VehicleId"" = ve.""Id""
                FROM ""Vehicles"" ve
                WHERE v.""VehiclePlate"" IS NOT NULL AND ve.""ComplexId"" = v.""ComplexId""
                  AND ve.""Plate"" = upper(regexp_replace(v.""VehiclePlate"", '[^A-Za-z0-9]', '', 'g'));");

            // Los eventos son inmutables: solo esta migración los toca, para enlazarlos con su vehículo
            migrationBuilder.Sql(@"
                ALTER TABLE ""AccessEvents"" DISABLE TRIGGER trg_access_events_immutable;

                UPDATE ""AccessEvents"" e SET ""VehicleId"" = v.""VehicleId""
                FROM ""Visits"" v
                WHERE e.""VisitId"" = v.""Id"" AND v.""VehicleId"" IS NOT NULL AND e.""Type"" IN (0, 1);

                ALTER TABLE ""AccessEvents"" ENABLE TRIGGER trg_access_events_immutable;");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "Visits");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessEvents_Vehicles_VehicleId",
                table: "AccessEvents",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Vehicles_VehicleId",
                table: "Visits",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessEvents_Vehicles_VehicleId",
                table: "AccessEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Vehicles_VehicleId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "VehicleAuthorizations");

            migrationBuilder.DropTable(
                name: "VehicleNotes");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Visits_VehicleId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_VehicleId_OccurredAt",
                table: "AccessEvents");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("1b2c3d4e-5f6a-4b7c-9d8e-0f1a2b3c4d5e"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("2c3d4e5f-6a7b-4c8d-ae9f-1a2b3c4d5e6f"));

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Visits");

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "Visits",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);
        }
    }
}
