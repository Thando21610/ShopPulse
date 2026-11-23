using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShopPulse.Api.Data;

#nullable disable

namespace ShopPulse.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261123123000_AddCashShiftConcurrency")]
public sealed partial class AddCashShiftConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Version", table: "CashShifts", type: "integer",
            nullable: false, defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_CashShifts_BusinessId_OperatorUserId",
            table: "CashShifts",
            columns: new[] { "BusinessId", "OperatorUserId" },
            unique: true,
            filter: "\"Status\" = 'Open'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CashShifts_BusinessId_OperatorUserId",
            table: "CashShifts");

        migrationBuilder.DropColumn(name: "Version", table: "CashShifts");
    }
}
