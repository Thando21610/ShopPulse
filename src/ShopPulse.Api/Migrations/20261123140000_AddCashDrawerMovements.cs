using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShopPulse.Api.Data;

#nullable disable

namespace ShopPulse.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261123140000_AddCashDrawerMovements")]
public sealed class AddCashDrawerMovements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CashMovements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                CashShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                OperatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CashMovements", x => x.Id);
                table.ForeignKey(
                    name: "FK_CashMovements_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CashMovements_CashShifts_CashShiftId",
                    column: x => x.CashShiftId,
                    principalTable: "CashShifts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CashMovements_BusinessId_CashShiftId_CreatedAtUtc",
            table: "CashMovements",
            columns: new[] { "BusinessId", "CashShiftId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CashMovements");
    }
}
