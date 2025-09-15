using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class TnxUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwnerBillingProfiles_FlatId",
                table: "OwnerBillingProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "TenantPayments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gateway",
                table: "TenantPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "TenantPayments",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TenantPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "ExpenseAllocationPayments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gateway",
                table: "ExpenseAllocationPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ExpenseAllocationPayments",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ExpenseAllocationPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPayments_IdempotencyKey",
                table: "TenantPayments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBillingProfiles_FlatId",
                table: "OwnerBillingProfiles",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAllocationPayments_IdempotencyKey",
                table: "ExpenseAllocationPayments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantPayments_IdempotencyKey",
                table: "TenantPayments");

            migrationBuilder.DropIndex(
                name: "IX_OwnerBillingProfiles_FlatId",
                table: "OwnerBillingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseAllocationPayments_IdempotencyKey",
                table: "ExpenseAllocationPayments");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
                table: "TenantPayments");

            migrationBuilder.DropColumn(
                name: "Gateway",
                table: "TenantPayments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "TenantPayments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TenantPayments");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
                table: "ExpenseAllocationPayments");

            migrationBuilder.DropColumn(
                name: "Gateway",
                table: "ExpenseAllocationPayments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ExpenseAllocationPayments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExpenseAllocationPayments");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBillingProfiles_FlatId",
                table: "OwnerBillingProfiles",
                column: "FlatId",
                unique: true);
        }
    }
}
