using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fauzora.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnAdminNote",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnDecisionAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnStatus",
                table: "orders",
                type: "text",
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnAdminNote",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReturnDecisionAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReturnStatus",
                table: "orders");
        }
    }
}
