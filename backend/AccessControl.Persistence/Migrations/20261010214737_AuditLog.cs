using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Residences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Residences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Persons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Persons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Complexes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Complexes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplexId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Changes = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Complexes",
                keyColumn: "Id",
                keyValue: new Guid("0f6a4c1e-7b52-4d0a-9e3c-5a1b2c3d4e5f"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("9452df48-5b92-40ef-829e-a315c01aadcb"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a509d9b3-9c77-4324-bc20-4a004b8c5abf"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b7e2a9c4-3d1f-4e6a-8b0c-9f5d2a1c7e30"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d4a1c3e5-6b7f-4081-92a3-b4c5d6e7f809"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d8fcac7d-c0ea-4cd5-8ad0-92b33953a142"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Group", "Icon", "IsActive", "Name", "Order", "Path", "Permission", "UpdatedAt", "UpdatedById" },
                values: new object[] { new Guid("e5b2d4f6-7c80-4192-a3b4-c5d6e7f8091a"), new DateTime(2025, 11, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Control", "ScrollText", true, "Bitácora", 90, "/access-control/audit", "audit.read", null, null });

            migrationBuilder.UpdateData(
                table: "Residences",
                keyColumn: "Id",
                keyValue: new Guid("8c885dcd-3734-45b6-9b7e-633351852714"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1a8553d6-0dee-4c79-aa40-2c41c68cd698"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("6d3b9f20-1c4e-4a7b-8f5d-2e9a0b1c3d4f"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ca682896-4e5f-4e3c-a3f0-f19627a71f93"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e790170c-4407-4cff-8358-8f6a09a46fad"),
                columns: new[] { "CreatedById", "UpdatedById" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ComplexId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "ComplexId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("e5b2d4f6-7c80-4192-a3b4-c5d6e7f8091a"));

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Complexes");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Complexes");
        }
    }
}
