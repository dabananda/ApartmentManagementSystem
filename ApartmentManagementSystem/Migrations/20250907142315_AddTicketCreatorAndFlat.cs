using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCreatorAndFlat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "MaintenanceTickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FlatId",
                table: "MaintenanceTickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_BuildingId_CreatedByUserId",
                table: "MaintenanceTickets",
                columns: new[] { "BuildingId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_BuildingId_FlatId_CreatedAt",
                table: "MaintenanceTickets",
                columns: new[] { "BuildingId", "FlatId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_BuildingId_CreatedByUserId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_BuildingId_FlatId_CreatedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "FlatId",
                table: "MaintenanceTickets");
        }
    }
}
