using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeatHold.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "holds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_holds_TransactionId",
                table: "holds",
                column: "TransactionId",
                unique: true,
                filter: "\"TransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_holds_TransactionId",
                table: "holds");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "holds");
        }
    }
}
