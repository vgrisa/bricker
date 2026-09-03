using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressComplement",
                table: "Listings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNumber",
                table: "Listings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Listings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Listings",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Listings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca01"),
                columns: new[] { "AddressComplement", "AddressNumber", "Neighborhood", "PostalCode", "Street" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca02"),
                columns: new[] { "AddressComplement", "AddressNumber", "Neighborhood", "PostalCode", "Street" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca03"),
                columns: new[] { "AddressComplement", "AddressNumber", "Neighborhood", "PostalCode", "Street" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressComplement",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "AddressNumber",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Listings");
        }
    }
}
