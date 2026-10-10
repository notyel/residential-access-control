using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VisitCheckedOutBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CheckedOutById",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CheckedOutById",
                table: "Visits",
                column: "CheckedOutById");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_CheckedOutById",
                table: "Visits",
                column: "CheckedOutById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_CheckedOutById",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CheckedOutById",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CheckedOutById",
                table: "Visits");
        }
    }
}
