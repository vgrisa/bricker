using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkListingsToSellers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "Listings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca01"),
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca02"),
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: new Guid("a304bbca-6477-4490-957b-10bc19e7ca03"),
                column: "SellerId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SellerId",
                table: "Listings",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_AspNetUsers_SellerId",
                table: "Listings",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_AspNetUsers_SellerId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_SellerId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Listings");
        }
    }
}
