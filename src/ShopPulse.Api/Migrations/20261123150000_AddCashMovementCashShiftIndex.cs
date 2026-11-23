using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopPulse.Api.Migrations
{
    public partial class AddCashMovementCashShiftIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashShiftId",
                table: "CashMovements",
                column: "CashShiftId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashShiftId",
                table: "CashMovements");
        }
    }
}
