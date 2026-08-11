using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    SellerDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsActive", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c01"), true, "Revestimentos", "revestimentos" },
                    { new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c02"), true, "Madeira", "madeira" },
                    { new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c03"), true, "Hidráulica", "hidraulica" },
                    { new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c04"), true, "Elétrica", "eletrica" },
                    { new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c05"), true, "Ferragens", "ferragens" }
                });

            migrationBuilder.InsertData(
                table: "Listings",
                columns: new[] { "Id", "CategoryId", "City", "Condition", "CreatedAtUtc", "Description", "Price", "Quantity", "SellerDisplayName", "State", "Status", "Title", "Unit", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("a304bbca-6477-4490-957b-10bc19e7ca01"), new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c01"), "Itajaí", 0, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Lote excedente de porcelanato acetinado, armazenado em local coberto.", 42m, 18m, "Construtora local", "SC", 1, "Porcelanato cinza 60 x 60", "m²", null },
                    { new Guid("a304bbca-6477-4490-957b-10bc19e7ca02"), new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c02"), "Balneário Camboriú", 0, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Portas novas, sem uso, excedentes de reforma residencial.", 380m, 3m, "Marcenaria parceira", "SC", 1, "Portas de madeira maciça", "unidade", null },
                    { new Guid("a304bbca-6477-4490-957b-10bc19e7ca03"), new Guid("9473e2aa-0fa8-4e01-b2cf-99781af54c01"), "Navegantes", 1, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Tijolos de solo-cimento disponíveis para retirada no local.", 1.25m, 800m, "Obra residencial", "SC", 1, "Tijolo ecológico", "unidade", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CategoryId",
                table: "Listings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Status_CategoryId_City_State",
                table: "Listings",
                columns: new[] { "Status", "CategoryId", "City", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
