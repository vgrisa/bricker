using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresProfileCompletion",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingInterestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Conversations_ListingInterests_ListingInterestId",
                        column: x => x.ListingInterestId,
                        principalTable: "ListingInterests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conversations_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ListingSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingInterestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingSales_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ListingSales_AspNetUsers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ListingSales_ListingInterests_ListingInterestId",
                        column: x => x.ListingInterestId,
                        principalTable: "ListingInterests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ListingSales_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RevieweeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReviews_AspNetUsers_RevieweeId",
                        column: x => x.RevieweeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReviews_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReviews_ListingSales_ListingSaleId",
                        column: x => x.ListingSaleId,
                        principalTable: "ListingSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_CreatedAtUtc",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BuyerId_LastMessageAtUtc",
                table: "Conversations",
                columns: new[] { "BuyerId", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ListingId",
                table: "Conversations",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ListingInterestId",
                table: "Conversations",
                column: "ListingInterestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_SellerId_LastMessageAtUtc",
                table: "Conversations",
                columns: new[] { "SellerId", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingSales_BuyerId",
                table: "ListingSales",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingSales_ListingId",
                table: "ListingSales",
                column: "ListingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingSales_ListingInterestId",
                table: "ListingSales",
                column: "ListingInterestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingSales_SellerId",
                table: "ListingSales",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_ListingSaleId_ReviewerId",
                table: "UserReviews",
                columns: new[] { "ListingSaleId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_RevieweeId_CreatedAtUtc",
                table: "UserReviews",
                columns: new[] { "RevieweeId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_ReviewerId",
                table: "UserReviews",
                column: "ReviewerId");

            // Every existing interest becomes a private conversation without
            // changing the original interest or listing data.
            migrationBuilder.Sql(
                """
                INSERT INTO [Conversations]
                    ([Id], [ListingInterestId], [ListingId], [BuyerId], [SellerId], [CreatedAtUtc], [LastMessageAtUtc])
                SELECT NEWID(), interest.[Id], interest.[ListingId], interest.[InterestedUserId], listing.[SellerId], interest.[CreatedAtUtc], NULL
                FROM [ListingInterests] AS interest
                INNER JOIN [Listings] AS listing ON listing.[Id] = interest.[ListingId]
                WHERE listing.[SellerId] IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [Conversations] AS conversation
                      WHERE conversation.[ListingInterestId] = interest.[Id]
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "UserReviews");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "ListingSales");

            migrationBuilder.DropColumn(
                name: "RequiresProfileCompletion",
                table: "AspNetUsers");
        }
    }
}
