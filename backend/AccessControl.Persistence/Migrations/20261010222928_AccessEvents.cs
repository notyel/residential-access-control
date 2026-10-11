using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_CheckedOutById",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_RegisteredById",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CheckedOutById",
                table: "Visits");

            migrationBuilder.AddColumn<Guid>(
                name: "VoidEventId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VehiclePlate",
                table: "Visits",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorizationId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntryEventId",
                table: "Visits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ExitEventId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Visits",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReasonNote",
                table: "Visits",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Verification",
                table: "Visits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PersonDocument = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    PersonKind = table.Column<int>(type: "integer", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehiclePlate = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessPoint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Verification = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmationChannel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsAdministrative = table.Column<bool>(type: "boolean", nullable: false),
                    VoidsEventId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisitAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizedByPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VisitorDocument = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeFrom = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    TimeTo = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DaysOfWeek = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitAuthorizations_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitAuthorizations_Persons_AuthorizedByPersonId",
                        column: x => x.AuthorizedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitAuthorizations_Persons_VisitorPersonId",
                        column: x => x.VisitorPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitAuthorizations_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                columns: new[] { "Name", "Order" },
                values: new object[] { "Visitantes", 5 });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[,]
                {
                    { new Guid("d0a7c9eb-c1d5-46e7-b809-1a2b3c4d5e6f"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "DoorOpen", true, "Validar visitantes", 4, "/access-control/gate", "access.register", null, null },
                    { new Guid("e1b8dafc-d2e6-47f8-8a1b-2b3c4d5e6f70"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Ticket", true, "Autorizaciones", 6, "/access-control/authorizations", "authorizations.read", null, null },
                    { new Guid("f2c9eb0d-e3f7-4809-9b2c-3c4d5e6f7081"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Ticket", true, "Autorizar visitantes", 12, "/access-control/my/authorizations", "authorizations.write_own", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AuthorizationId",
                table: "Visits",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ComplexId_ExitEventId",
                table: "Visits",
                columns: new[] { "ComplexId", "ExitEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_EntryEventId",
                table: "Visits",
                column: "EntryEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_ExitEventId",
                table: "Visits",
                column: "ExitEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_ComplexId_OccurredAt",
                table: "AccessEvents",
                columns: new[] { "ComplexId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_ComplexId_VehiclePlate",
                table: "AccessEvents",
                columns: new[] { "ComplexId", "VehiclePlate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_OperatorId",
                table: "AccessEvents",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_PersonId",
                table: "AccessEvents",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_UnitId_OccurredAt",
                table: "AccessEvents",
                columns: new[] { "UnitId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_VisitId",
                table: "AccessEvents",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_VoidsEventId",
                table: "AccessEvents",
                column: "VoidsEventId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAuthorizations_AuthorizedByPersonId",
                table: "VisitAuthorizations",
                column: "AuthorizedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitAuthorizations_ComplexId_EndDate",
                table: "VisitAuthorizations",
                columns: new[] { "ComplexId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitAuthorizations_UnitId_Status",
                table: "VisitAuthorizations",
                columns: new[] { "UnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitAuthorizations_VisitorPersonId",
                table: "VisitAuthorizations",
                column: "VisitorPersonId");

            // ---- Conversión de las visitas existentes: cada una pasa a tener sus eventos ----

            // La entrada era la fecha de creación de la visita; quien la registró es el operador.
            // Verification = 2 (aprobada por portería): antes no existían las autorizaciones previas
            migrationBuilder.Sql(@"
                INSERT INTO ""AccessEvents"" (""Id"", ""ComplexId"", ""Type"", ""OccurredAt"", ""PersonId"", ""PersonName"", ""PersonDocument"",
                                            ""PersonKind"", ""UnitId"", ""VisitId"", ""VehiclePlate"", ""OperatorId"", ""Verification"", ""IsAdministrative"")
                SELECT gen_random_uuid(), v.""ComplexId"", 0, v.""CreatedAt"", v.""PersonId"", p.""FirstName"" || ' ' || p.""LastName"",
                       p.""DocumentType"" || ' ' || p.""DocumentNumber"", 0, v.""UnitId"", v.""Id"", v.""VehiclePlate"", v.""RegisteredById"", 2, false
                FROM ""Visits"" v
                JOIN ""Persons"" p ON p.""Id"" = v.""PersonId"";

                UPDATE ""Visits"" v SET ""EntryEventId"" = e.""Id"", ""Verification"" = 2, ""Reason"" = 'otro'
                FROM ""AccessEvents"" e
                WHERE e.""VisitId"" = v.""Id"" AND e.""Type"" = 0;");

            // La salida, si la tenía, conserva su hora y quien la marcó
            migrationBuilder.Sql(@"
                INSERT INTO ""AccessEvents"" (""Id"", ""ComplexId"", ""Type"", ""OccurredAt"", ""PersonId"", ""PersonName"", ""PersonDocument"",
                                            ""PersonKind"", ""UnitId"", ""VisitId"", ""VehiclePlate"", ""OperatorId"", ""IsAdministrative"")
                SELECT gen_random_uuid(), v.""ComplexId"", 1, v.""CheckOut"", v.""PersonId"", p.""FirstName"" || ' ' || p.""LastName"",
                       p.""DocumentType"" || ' ' || p.""DocumentNumber"", 0, v.""UnitId"", v.""Id"", v.""VehiclePlate"",
                       COALESCE(v.""CheckedOutById"", v.""RegisteredById""), false
                FROM ""Visits"" v
                JOIN ""Persons"" p ON p.""Id"" = v.""PersonId""
                WHERE v.""CheckOut"" IS NOT NULL;

                UPDATE ""Visits"" v SET ""ExitEventId"" = e.""Id""
                FROM ""AccessEvents"" e
                WHERE e.""VisitId"" = v.""Id"" AND e.""Type"" = 1;");

            migrationBuilder.DropColumn(
                name: "CheckOut",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CheckedOutById",
                table: "Visits");

            // Los eventos de acceso solo se agregan: la base rechaza cualquier UPDATE o DELETE,
            // venga de donde venga
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION access_events_immutable() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Los eventos de acceso no se modifican ni se borran';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_access_events_immutable
                    BEFORE UPDATE OR DELETE ON ""AccessEvents""
                    FOR EACH ROW EXECUTE FUNCTION access_events_immutable();");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_AccessEvents_EntryEventId",
                table: "Visits",
                column: "EntryEventId",
                principalTable: "AccessEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_AccessEvents_ExitEventId",
                table: "Visits",
                column: "ExitEventId",
                principalTable: "AccessEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_RegisteredById",
                table: "Visits",
                column: "RegisteredById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_VisitAuthorizations_AuthorizationId",
                table: "Visits",
                column: "AuthorizationId",
                principalTable: "VisitAuthorizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_AccessEvents_EntryEventId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_AccessEvents_ExitEventId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_RegisteredById",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_VisitAuthorizations_AuthorizationId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "AccessEvents");

            migrationBuilder.DropTable(
                name: "VisitAuthorizations");

            migrationBuilder.DropIndex(
                name: "IX_Visits_AuthorizationId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ComplexId_ExitEventId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_EntryEventId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ExitEventId",
                table: "Visits");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d0a7c9eb-c1d5-46e7-b809-1a2b3c4d5e6f"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("e1b8dafc-d2e6-47f8-8a1b-2b3c4d5e6f70"));

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("f2c9eb0d-e3f7-4809-9b2c-3c4d5e6f7081"));

            migrationBuilder.DropColumn(
                name: "AuthorizationId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "EntryEventId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ExitEventId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ReasonNote",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Verification",
                table: "Visits");

            migrationBuilder.RenameColumn(
                name: "VoidEventId",
                table: "Visits",
                newName: "CheckedOutById");

            migrationBuilder.AlterColumn<string>(
                name: "VehiclePlate",
                table: "Visits",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOut",
                table: "Visits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                columns: new[] { "Name", "Order" },
                values: new object[] { "Visitas", 4 });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CheckedOutById",
                table: "Visits",
                column: "CheckedOutById");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Persons_PersonId",
                table: "Visits",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_CheckedOutById",
                table: "Visits",
                column: "CheckedOutById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_RegisteredById",
                table: "Visits",
                column: "RegisteredById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
