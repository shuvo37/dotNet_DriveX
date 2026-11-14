using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveX.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "transaction_id",
                table: "payment",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_id",
                table: "payment",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_transaction_id",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "payment");
        }
    }
}
