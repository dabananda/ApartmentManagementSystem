using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class NewExpenseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseAllocationPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CommonBillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAllocationPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseAllocationPayments_ExpenseAllocations_ExpenseAllocationId",
                        column: x => x.ExpenseAllocationId,
                        principalTable: "ExpenseAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAllocationPayments_CommonBillId_OwnerId_PaymentDate",
                table: "ExpenseAllocationPayments",
                columns: new[] { "CommonBillId", "OwnerId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAllocationPayments_ExpenseAllocationId",
                table: "ExpenseAllocationPayments",
                column: "ExpenseAllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseAllocationPayments");
        }
    }
}
