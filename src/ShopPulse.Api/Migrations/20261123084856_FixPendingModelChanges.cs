using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopPulse.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Reason"" TYPE text USING ""Reason""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Quantity"" TYPE integer USING ""Quantity""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""ProductId"" TYPE uuid USING ""ProductId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Notes"" TYPE text USING ""Notes""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""ActorUserId"" TYPE uuid USING ""ActorUserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Role"" TYPE character varying(32) USING ""Role""::character varying(32);");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""PasswordHash"" TYPE text USING ""PasswordHash""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""LockoutEndUtc"" TYPE timestamp with time zone USING ""LockoutEndUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::boolean;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""FailedLoginCount"" TYPE integer USING ""FailedLoginCount""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Email"" TYPE character varying(256) USING ""Email""::character varying(256);");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""DisplayName"" TYPE text USING ""DisplayName""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""CanAdjustStock"" TYPE boolean USING ""CanAdjustStock""::boolean;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ReferenceId"" TYPE uuid USING ""ReferenceId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""Reason"" TYPE text USING ""Reason""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""QuantityChange"" TYPE integer USING ""QuantityChange""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ProductId"" TYPE uuid USING ""ProductId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""MovementType"" TYPE text USING ""MovementType""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ActorUserId"" TYPE uuid USING ""ActorUserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Version"" TYPE integer USING ""Version""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Supplier"" TYPE text USING ""Supplier""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""RemainingQuantity"" TYPE integer USING ""RemainingQuantity""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ReceivedQuantity"" TYPE integer USING ""ReceivedQuantity""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ReceivedAtUtc"" TYPE timestamp with time zone USING ""ReceivedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ProductId"" TYPE uuid USING ""ProductId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ExpiryDate"" TYPE date USING ""ExpiryDate""::date;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""BatchReference"" TYPE text USING ""BatchReference""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""Status"" TYPE text USING ""Status""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""ReceiptNumber"" TYPE character varying(32) USING ""ReceiptNumber""::character varying(32);");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""PaymentMethod"" TYPE text USING ""PaymentMethod""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""OperatorUserId"" TYPE uuid USING ""OperatorUserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""IdempotencyKey"" TYPE text USING ""IdempotencyKey""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""SkuSnapshot"" TYPE text USING ""SkuSnapshot""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""SaleId"" TYPE uuid USING ""SaleId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""Quantity"" TYPE integer USING ""Quantity""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""ProductNameSnapshot"" TYPE text USING ""ProductNameSnapshot""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""ProductId"" TYPE uuid USING ""ProductId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""UserAgent"" TYPE text USING ""UserAgent""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""TokenHash"" TYPE character varying(128) USING ""TokenHash""::character varying(128);");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""RevokedReason"" TYPE text USING ""RevokedReason""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""RevokedAtUtc"" TYPE timestamp with time zone USING ""RevokedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""ReplacedByTokenId"" TYPE uuid USING ""ReplacedByTokenId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""FamilyId"" TYPE uuid USING ""FamilyId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""FamilyAbsoluteExpiryUtc"" TYPE timestamp with time zone USING ""FamilyAbsoluteExpiryUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""ExpiresAtUtc"" TYPE timestamp with time zone USING ""ExpiresAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""CreatedByIp"" TYPE text USING ""CreatedByIp""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Sku"" TYPE character varying(64) USING ""Sku""::character varying(64);");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Name"" TYPE character varying(200) USING ""Name""::character varying(200);");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""MinimumStockLevel"" TYPE integer USING ""MinimumStockLevel""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::boolean;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""HasExpiry"" TYPE boolean USING ""HasExpiry""::boolean;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Category"" TYPE text USING ""Category""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""StatusCode"" TYPE integer USING ""StatusCode""::integer;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""ResponseJson"" TYPE text USING ""ResponseJson""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""RequestHash"" TYPE text USING ""RequestHash""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""Key"" TYPE character varying(128) USING ""Key""::character varying(128);");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""ExpiresAtUtc"" TYPE timestamp with time zone USING ""ExpiresAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""VarianceReason"" TYPE text USING ""VarianceReason""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""Status"" TYPE text USING ""Status""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""OperatorUserId"" TYPE uuid USING ""OperatorUserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""OpenedAtUtc"" TYPE timestamp with time zone USING ""OpenedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""ClosedAtUtc"" TYPE timestamp with time zone USING ""ClosedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Name"" TYPE character varying(200) USING ""Name""::character varying(200);");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::boolean;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Address"" TYPE text USING ""Address""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""EntityType"" TYPE text USING ""EntityType""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""EntityId"" TYPE uuid USING ""EntityId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""DetailsJson"" TYPE text USING ""DetailsJson""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""CreatedAtUtc"" TYPE timestamp with time zone USING ""CreatedAtUtc""::timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""BusinessId"" TYPE uuid USING ""BusinessId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""ActorUserId"" TYPE uuid USING ""ActorUserId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""Action"" TYPE text USING ""Action""::text;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Reason"" TYPE TEXT USING ""Reason""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Quantity"" TYPE INTEGER USING ""Quantity""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""ProductId"" TYPE TEXT USING ""ProductId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Notes"" TYPE TEXT USING ""Notes""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""ActorUserId"" TYPE TEXT USING ""ActorUserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""WasteRecords"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Role"" TYPE TEXT USING ""Role""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""PasswordHash"" TYPE TEXT USING ""PasswordHash""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""LockoutEndUtc"" TYPE TEXT USING ""LockoutEndUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""IsActive"" TYPE INTEGER USING ""IsActive""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""FailedLoginCount"" TYPE INTEGER USING ""FailedLoginCount""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Email"" TYPE TEXT USING ""Email""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""DisplayName"" TYPE TEXT USING ""DisplayName""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""CanAdjustStock"" TYPE INTEGER USING ""CanAdjustStock""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ReferenceId"" TYPE TEXT USING ""ReferenceId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""Reason"" TYPE TEXT USING ""Reason""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""QuantityChange"" TYPE INTEGER USING ""QuantityChange""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ProductId"" TYPE TEXT USING ""ProductId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""MovementType"" TYPE TEXT USING ""MovementType""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""ActorUserId"" TYPE TEXT USING ""ActorUserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockMovements"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Version"" TYPE INTEGER USING ""Version""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Supplier"" TYPE TEXT USING ""Supplier""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""RemainingQuantity"" TYPE INTEGER USING ""RemainingQuantity""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ReceivedQuantity"" TYPE INTEGER USING ""ReceivedQuantity""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ReceivedAtUtc"" TYPE TEXT USING ""ReceivedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ProductId"" TYPE TEXT USING ""ProductId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""ExpiryDate"" TYPE TEXT USING ""ExpiryDate""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""BatchReference"" TYPE TEXT USING ""BatchReference""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""StockBatches"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""Status"" TYPE TEXT USING ""Status""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""ReceiptNumber"" TYPE TEXT USING ""ReceiptNumber""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""PaymentMethod"" TYPE TEXT USING ""PaymentMethod""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""OperatorUserId"" TYPE TEXT USING ""OperatorUserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""IdempotencyKey"" TYPE TEXT USING ""IdempotencyKey""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Sales"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""StockBatchId"" TYPE uuid USING ""StockBatchId""::uuid;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""SkuSnapshot"" TYPE TEXT USING ""SkuSnapshot""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""SaleId"" TYPE TEXT USING ""SaleId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""Quantity"" TYPE INTEGER USING ""Quantity""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""ProductNameSnapshot"" TYPE TEXT USING ""ProductNameSnapshot""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""ProductId"" TYPE TEXT USING ""ProductId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""SaleItems"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""UserId"" TYPE TEXT USING ""UserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""UserAgent"" TYPE TEXT USING ""UserAgent""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""TokenHash"" TYPE TEXT USING ""TokenHash""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""RevokedReason"" TYPE TEXT USING ""RevokedReason""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""RevokedAtUtc"" TYPE TEXT USING ""RevokedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""ReplacedByTokenId"" TYPE TEXT USING ""ReplacedByTokenId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""FamilyId"" TYPE TEXT USING ""FamilyId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""FamilyAbsoluteExpiryUtc"" TYPE TEXT USING ""FamilyAbsoluteExpiryUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""ExpiresAtUtc"" TYPE TEXT USING ""ExpiresAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""CreatedByIp"" TYPE TEXT USING ""CreatedByIp""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Sku"" TYPE TEXT USING ""Sku""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Name"" TYPE TEXT USING ""Name""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""MinimumStockLevel"" TYPE INTEGER USING ""MinimumStockLevel""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""IsActive"" TYPE INTEGER USING ""IsActive""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""HasExpiry"" TYPE INTEGER USING ""HasExpiry""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Category"" TYPE TEXT USING ""Category""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Products"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""StatusCode"" TYPE INTEGER USING ""StatusCode""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""ResponseJson"" TYPE TEXT USING ""ResponseJson""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""RequestHash"" TYPE TEXT USING ""RequestHash""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""Key"" TYPE TEXT USING ""Key""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""ExpiresAtUtc"" TYPE TEXT USING ""ExpiresAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""IdempotencyRecords"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""VarianceReason"" TYPE TEXT USING ""VarianceReason""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""Status"" TYPE TEXT USING ""Status""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""OperatorUserId"" TYPE TEXT USING ""OperatorUserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""OpenedAtUtc"" TYPE TEXT USING ""OpenedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""ClosedAtUtc"" TYPE TEXT USING ""ClosedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""CashShifts"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Name"" TYPE TEXT USING ""Name""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""IsActive"" TYPE INTEGER USING ""IsActive""::INTEGER;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Address"" TYPE TEXT USING ""Address""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""Businesses"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""EntityType"" TYPE TEXT USING ""EntityType""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""EntityId"" TYPE TEXT USING ""EntityId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""DetailsJson"" TYPE TEXT USING ""DetailsJson""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""CreatedAtUtc"" TYPE TEXT USING ""CreatedAtUtc""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""BusinessId"" TYPE TEXT USING ""BusinessId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""ActorUserId"" TYPE TEXT USING ""ActorUserId""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""Action"" TYPE TEXT USING ""Action""::TEXT;");

            migrationBuilder.Sql(@"ALTER TABLE ""AuditEvents"" ALTER COLUMN ""Id"" TYPE TEXT USING ""Id""::TEXT;");
        }
    }
}
