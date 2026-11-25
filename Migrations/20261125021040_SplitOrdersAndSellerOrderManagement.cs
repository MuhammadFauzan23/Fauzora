using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fauzora.Migrations
{
    /// <inheritdoc />
    public partial class SplitOrdersAndSellerOrderManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some development databases may already contain SellerId from an earlier manual/partial rollout.
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS \"CheckoutGroupId\" uuid;");
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS \"SellerId\" text;");
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS \"SellerName\" text;");
            migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN \"SellerId\" DROP NOT NULL;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_orders_CheckoutGroupId\" ON orders (\"CheckoutGroupId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_orders_SellerId_CreatedAt\" ON orders (\"SellerId\", \"CreatedAt\");");
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_orders_users_SellerId'
                          AND conrelid = 'orders'::regclass
                    ) THEN
                        ALTER TABLE orders
                            ADD CONSTRAINT "FK_orders_users_SellerId"
                            FOREIGN KEY ("SellerId") REFERENCES users ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_SellerId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_CheckoutGroupId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_SellerId_CreatedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CheckoutGroupId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "orders");
        }
    }
}
