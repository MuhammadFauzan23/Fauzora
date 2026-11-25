using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fauzora.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileAddressesAndOrderSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressCity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressContactName",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressDeliveryNote",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressDistrict",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressPhone",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressPostalCode",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressProvince",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressStreet",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressVillage",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressCity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressContactName",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressDeliveryNote",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressDistrict",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressPhone",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressPostalCode",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressProvince",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressStreet",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressVillage",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressCity",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressContactName",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressDeliveryNote",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressDistrict",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressPhone",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressPostalCode",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressProvince",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressStreet",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddressVillage",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressCity",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressContactName",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressDeliveryNote",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressDistrict",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressPhone",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressPostalCode",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressProvince",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressStreet",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerAddressVillage",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAddressCity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressContactName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressDeliveryNote",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressDistrict",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressPhone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressPostalCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressProvince",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressStreet",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressVillage",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressCity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressContactName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressDeliveryNote",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressDistrict",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressPhone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressPostalCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressProvince",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressStreet",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SellerAddressVillage",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerAddressCity",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressContactName",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressDeliveryNote",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressDistrict",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressPhone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressPostalCode",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressProvince",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressStreet",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerAddressVillage",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressCity",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressContactName",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressDeliveryNote",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressDistrict",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressPhone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressPostalCode",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressProvince",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressStreet",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SellerAddressVillage",
                table: "orders");
        }
    }
}
