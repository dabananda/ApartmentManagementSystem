using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FlatBillingProfileAndTenantBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantBills_Tenants_TenantId",
                table: "TenantBills");

            migrationBuilder.DropIndex(
                name: "IX_TenantBills_FlatId_Year_Month",
                table: "TenantBills");

            migrationBuilder.DropIndex(
                name: "IX_TenantBills_TenantId",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "CommonBillAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "ElectricityAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "GasAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "OtherAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "RentAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "ServiceChargeAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "TenantBills");

            migrationBuilder.RenameColumn(
                name: "WaterAmount",
                table: "TenantBills",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "TenantBills",
                newName: "BillDate");

            migrationBuilder.AddColumn<string>(
                name: "TenantUserId",
                table: "TenantBills",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TenantBills",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FlatBillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlatBillingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlatBillingProfiles_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAssignments_AspNetUsers_TenantUserId",
                        column: x => x.TenantUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantAssignments_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantBillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPayments_TenantBills_TenantBillId",
                        column: x => x.TenantBillId,
                        principalTable: "TenantBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantBills_FlatId",
                table: "TenantBills",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantBills_TenantUserId_BillDate",
                table: "TenantBills",
                columns: new[] { "TenantUserId", "BillDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FlatBillingProfiles_FlatId",
                table: "FlatBillingProfiles",
                column: "FlatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAssignments_FlatId_TenantUserId_StartDate",
                table: "TenantAssignments",
                columns: new[] { "FlatId", "TenantUserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAssignments_TenantUserId",
                table: "TenantAssignments",
                column: "TenantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPayments_TenantBillId_PaymentDate",
                table: "TenantPayments",
                columns: new[] { "TenantBillId", "PaymentDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_TenantBills_AspNetUsers_TenantUserId",
                table: "TenantBills",
                column: "TenantUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantBills_AspNetUsers_TenantUserId",
                table: "TenantBills");

            migrationBuilder.DropTable(
                name: "FlatBillingProfiles");

            migrationBuilder.DropTable(
                name: "TenantAssignments");

            migrationBuilder.DropTable(
                name: "TenantPayments");

            migrationBuilder.DropIndex(
                name: "IX_TenantBills_FlatId",
                table: "TenantBills");

            migrationBuilder.DropIndex(
                name: "IX_TenantBills_TenantUserId_BillDate",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "TenantUserId",
                table: "TenantBills");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TenantBills");

            migrationBuilder.RenameColumn(
                name: "BillDate",
                table: "TenantBills",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "TenantBills",
                newName: "WaterAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "CommonBillAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TenantBills",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GasAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "TenantBills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RentAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargeAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TenantBills",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "TenantBills",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "TenantBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "TenantBills",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "FK_TenantBills_Tenants_TenantId",
                table: "TenantBills",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
