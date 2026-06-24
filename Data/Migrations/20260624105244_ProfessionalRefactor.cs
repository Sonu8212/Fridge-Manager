using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FridgeManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "FridgeItems");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FridgeItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FridgeItems_ExpiryDate",
                table: "FridgeItems",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_FridgeItems_UserId",
                table: "FridgeItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FridgeItems_ExpiryDate",
                table: "FridgeItems");

            migrationBuilder.DropIndex(
                name: "IX_FridgeItems_UserId",
                table: "FridgeItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FridgeItems");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "FridgeItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
