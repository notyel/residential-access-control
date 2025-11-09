using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VisitVisitorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Visits");

            migrationBuilder.RenameColumn(
                name: "VisitorDocument",
                table: "Visits",
                newName: "VisitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VisitorId",
                table: "Visits",
                newName: "VisitorDocument");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Visits",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
