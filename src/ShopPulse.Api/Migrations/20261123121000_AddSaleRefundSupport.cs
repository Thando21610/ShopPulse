using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShopPulse.Api.Data;

#nullable disable

namespace ShopPulse.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261123121000_AddSaleRefundSupport")]
public sealed class AddSaleRefundSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CanRefundSales", table: "Users", type: "boolean",
            nullable: false, defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "RefundAmount", table: "Sales", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, defaultValue: 0m);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RefundedAtUtc", table: "Sales", type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RefundedByUserId", table: "Sales", type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RefundReason", table: "Sales", type: "character varying(500)",
            maxLength: 500, nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RefundPaymentMethod", table: "Sales", type: "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "RefundRestocked", table: "Sales", type: "boolean",
            nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CanRefundSales", table: "Users");
        migrationBuilder.DropColumn(name: "RefundAmount", table: "Sales");
        migrationBuilder.DropColumn(name: "RefundedAtUtc", table: "Sales");
        migrationBuilder.DropColumn(name: "RefundedByUserId", table: "Sales");
        migrationBuilder.DropColumn(name: "RefundReason", table: "Sales");
        migrationBuilder.DropColumn(name: "RefundPaymentMethod", table: "Sales");
        migrationBuilder.DropColumn(name: "RefundRestocked", table: "Sales");
    }
}
