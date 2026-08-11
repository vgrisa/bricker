using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca01"),
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca02"),
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca03"),
                column: "ImageUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Listings");
        }
    }
}
