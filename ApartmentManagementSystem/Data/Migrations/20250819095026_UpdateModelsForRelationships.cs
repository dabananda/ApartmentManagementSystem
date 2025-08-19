using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flats_AspNetUsers_OwnerId",
                table: "Flats");

            migrationBuilder.DropIndex(
                name: "IX_Flats_BuildingId",
                table: "Flats");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Flats",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Fullname",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Flats_BuildingId_FlatNumber",
                table: "Flats",
                columns: new[] { "BuildingId", "FlatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Name",
                table: "Buildings",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BuildingId",
                table: "AspNetUsers",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Buildings_BuildingId",
                table: "AspNetUsers",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Flats_AspNetUsers_OwnerId",
                table: "Flats",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Buildings_BuildingId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Flats_AspNetUsers_OwnerId",
                table: "Flats");

            migrationBuilder.DropIndex(
                name: "IX_Flats_BuildingId_FlatNumber",
                table: "Flats");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_Name",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BuildingId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Flats",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fullname",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Flats_BuildingId",
                table: "Flats",
                column: "BuildingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flats_AspNetUsers_OwnerId",
                table: "Flats",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
