using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bricker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestCenterSystemMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ChatMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                INSERT INTO [ChatMessages] ([Id], [ConversationId], [SenderId], [Body], [Type], [CreatedAtUtc], [ReadAtUtc])
                SELECT NEWID(), conversation.[Id], NULL,
                    CASE listing.[Status]
                        WHEN 2 THEN N'Este material foi reservado.'
                        WHEN 3 THEN N'Este material foi vendido.'
                        WHEN 4 THEN N'Este material foi inativado.'
                    END,
                    1,
                    COALESCE(listing.[UpdatedAtUtc], listing.[CreatedAtUtc]),
                    NULL
                FROM [Conversations] AS conversation
                INNER JOIN [Listings] AS listing ON listing.[Id] = conversation.[ListingId]
                WHERE listing.[Status] IN (2, 3, 4)
                  AND NOT EXISTS (
                      SELECT 1 FROM [ChatMessages] AS message
                      WHERE message.[ConversationId] = conversation.[Id] AND message.[Type] = 1
                  );

                UPDATE conversation
                SET [LastMessageAtUtc] = latest.[CreatedAtUtc]
                FROM [Conversations] AS conversation
                CROSS APPLY (
                    SELECT TOP (1) message.[CreatedAtUtc]
                    FROM [ChatMessages] AS message
                    WHERE message.[ConversationId] = conversation.[Id]
                    ORDER BY message.[CreatedAtUtc] DESC
                ) AS latest
                WHERE conversation.[LastMessageAtUtc] IS NULL OR conversation.[LastMessageAtUtc] < latest.[CreatedAtUtc];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [ChatMessages] WHERE [SenderId] IS NULL;");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ChatMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
