using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerBillingAndTenantBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Flats_FlatId",
                table: "Tenants");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantBillId",
                table: "Rents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OwnerBillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ElectricityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GasAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WaterAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommonBillAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceChargeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerBillingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerBillingProfiles_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantBills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ElectricityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GasAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WaterAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommonBillAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceChargeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantBills_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantBills_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rents_TenantBillId",
                table: "Rents",
                column: "TenantBillId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerBillingProfiles_FlatId",
                table: "OwnerBillingProfiles",
                column: "FlatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantBills_FlatId_Year_Month",
                table: "TenantBills",
                columns: new[] { "FlatId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantBills_TenantId",
                table: "TenantBills",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rents_TenantBills_TenantBillId",
                table: "Rents",
                column: "TenantBillId",
                principalTable: "TenantBills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Flats_FlatId",
                table: "Tenants",
                column: "FlatId",
                principalTable: "Flats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rents_TenantBills_TenantBillId",
                table: "Rents");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Flats_FlatId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "OwnerBillingProfiles");

            migrationBuilder.DropTable(
                name: "TenantBills");

            migrationBuilder.DropIndex(
                name: "IX_Rents_TenantBillId",
                table: "Rents");

            migrationBuilder.DropColumn(
                name: "TenantBillId",
                table: "Rents");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Flats_FlatId",
                table: "Tenants",
                column: "FlatId",
                principalTable: "Flats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
