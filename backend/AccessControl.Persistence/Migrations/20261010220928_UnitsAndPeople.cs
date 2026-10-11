using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnitsAndPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Residences_ResidenceId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "PersonType",
                table: "Persons");

            migrationBuilder.RenameColumn(
                name: "ResidenceId",
                table: "Visits",
                newName: "UnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Visits_ResidenceId",
                table: "Visits",
                newName: "IX_Visits_UnitId");

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Catalog = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogItems_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Structures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Structures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Structures_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Structures_Structures_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StructureId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Area = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Units_Structures_StructureId",
                        column: x => x.StructureId,
                        principalTable: "Structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Occupancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Relation = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occupancies", x => x.Id);
                    table.CheckConstraint("CK_Occupancies_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_Occupancies_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occupancies_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occupancies_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOwnerships", x => x.Id);
                    table.CheckConstraint("CK_UnitOwnerships_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_UnitOwnerships_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitOwnerships_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitOwnerships_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                column: "Order",
                value: 51);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809"),
                column: "Order",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                columns: new[] { "Name", "Path", "Permission" },
                values: new object[] { "Propietarios y residentes", "/access-control/people", "people.read" });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[] { new Guid("a7d4f6b8-9ea2-43b4-85d6-e7f8091a2b3c"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Building2", true, "Unidades", 2, "/access-control/units", "units.read", null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                column: "PersonId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f"),
                column: "PersonId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                column: "PersonId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                column: "PersonId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonId",
                table: "Users",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_ComplexId_Catalog_Code",
                table: "CatalogItems",
                columns: new[] { "ComplexId", "Catalog", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occupancies_ComplexId",
                table: "Occupancies",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_Occupancies_PersonId",
                table: "Occupancies",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Occupancies_UnitId_PersonId",
                table: "Occupancies",
                columns: new[] { "UnitId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Structures_ComplexId_ParentId_Name",
                table: "Structures",
                columns: new[] { "ComplexId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Structures_ParentId",
                table: "Structures",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwnerships_ComplexId",
                table: "UnitOwnerships",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwnerships_PersonId",
                table: "UnitOwnerships",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwnerships_UnitId_PersonId",
                table: "UnitOwnerships",
                columns: new[] { "UnitId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_ComplexId_Code",
                table: "Units",
                columns: new[] { "ComplexId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_StructureId",
                table: "Units",
                column: "StructureId");

            // ---- Conversión de los datos existentes (antes de crear las llaves foráneas) ----

            // Cada residencia pasa a ser una unidad con el mismo id, así las visitas conservan su destino
            migrationBuilder.Sql(@"
                INSERT INTO ""Units"" (""Id"", ""ComplexId"", ""Code"", ""Type"", ""Label"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"")
                SELECT r.""Id"", r.""ComplexId"", r.""Identifier"", 'apartamento', r.""Identifier"", r.""CreatedAt"", r.""UpdatedAt"", r.""IsActive""
                FROM ""Residences"" r;");

            // Cada usuario dueño de una residencia pasa a tener su persona. No se conocía su documento:
            // queda uno provisional (USR-…) con tipo NA, para completarlo desde la ficha de la persona
            migrationBuilder.Sql(@"
                INSERT INTO ""Persons"" (""Id"", ""ComplexId"", ""FirstName"", ""LastName"", ""DocumentType"", ""DocumentNumber"", ""Email"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), u.""ComplexId"", u.""FirstName"", u.""LastName"", 'NA', 'USR-' || left(u.""Id""::text, 8), u.""Email"", now(), true
                FROM ""Users"" u
                WHERE u.""ComplexId"" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM ""Residences"" r WHERE r.""OwnerId"" = u.""Id"");

                UPDATE ""Users"" u SET ""PersonId"" = p.""Id""
                FROM ""Persons"" p
                WHERE p.""ComplexId"" = u.""ComplexId"" AND p.""DocumentNumber"" = 'USR-' || left(u.""Id""::text, 8);");

            // El dueño de cada residencia queda como propietario vigente de la unidad desde que se registró
            migrationBuilder.Sql(@"
                INSERT INTO ""UnitOwnerships"" (""Id"", ""ComplexId"", ""UnitId"", ""PersonId"", ""StartDate"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), r.""ComplexId"", r.""Id"", u.""PersonId"", r.""CreatedAt""::date, now(), true
                FROM ""Residences"" r
                JOIN ""Users"" u ON u.""Id"" = r.""OwnerId""
                WHERE u.""PersonId"" IS NOT NULL;");

            migrationBuilder.DropTable(
                name: "Residences");

            // Una misma persona no puede tener dos titularidades ni dos ocupaciones que se crucen en la
            // misma unidad. Lo garantiza la base, también ante dos peticiones simultáneas
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE ""UnitOwnerships"" ADD CONSTRAINT ""EX_UnitOwnerships_NoOverlap""
                    EXCLUDE USING gist (""UnitId"" WITH =, ""PersonId"" WITH =, daterange(""StartDate"", ""EndDate"", '[]') WITH &&);

                ALTER TABLE ""Occupancies"" ADD CONSTRAINT ""EX_Occupancies_NoOverlap""
                    EXCLUDE USING gist (""UnitId"" WITH =, ""PersonId"" WITH =, daterange(""StartDate"", ""EndDate"", '[]') WITH &&)
                    WHERE (""Status"" IN (0, 1));");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Persons_PersonId",
                table: "Users",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Units_UnitId",
                table: "Visits",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Persons_PersonId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Units_UnitId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "Occupancies");

            migrationBuilder.DropTable(
                name: "UnitOwnerships");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Structures");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonId",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a7d4f6b8-9ea2-43b4-85d6-e7f8091a2b3c"));

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "UnitId",
                table: "Visits",
                newName: "ResidenceId");

            migrationBuilder.RenameIndex(
                name: "IX_Visits_UnitId",
                table: "Visits",
                newName: "IX_Visits_ResidenceId");

            migrationBuilder.AddColumn<int>(
                name: "PersonType",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Residences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Residences_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Residences_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                column: "Order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                column: "Order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809"),
                column: "Order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                columns: new[] { "Name", "Path", "Permission" },
                values: new object[] { "Residentes", "/access-control/residents", "users.read" });

            migrationBuilder.InsertData(
                table: "Residences",
                columns: new[] { "Id", "ComplexId", "CreatedAt", "CreatedById", "Identifier", "IsActive", "OwnerId", "UpdatedAt", "UpdatedById" },
                values: new object[] { new Guid("8c885dcd-3734-45b6-9b7e-633351852714"), new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "RES-001", true, new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Residences_ComplexId_Identifier",
                table: "Residences",
                columns: new[] { "ComplexId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Residences_OwnerId",
                table: "Residences",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Residences_ResidenceId",
                table: "Visits",
                column: "ResidenceId",
                principalTable: "Residences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
