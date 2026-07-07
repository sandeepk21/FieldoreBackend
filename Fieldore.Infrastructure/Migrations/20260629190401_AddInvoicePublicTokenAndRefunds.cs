using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePublicTokenAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_refund",
                table: "payment_records",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "refunded_payment_id",
                table: "payment_records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_token",
                table: "invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_public_token",
                table: "invoices",
                column: "public_token",
                unique: true,
                filter: "[public_token] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_public_token",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "is_refund",
                table: "payment_records");

            migrationBuilder.DropColumn(
                name: "refunded_payment_id",
                table: "payment_records");

            migrationBuilder.DropColumn(
                name: "public_token",
                table: "invoices");
        }
    }
}
