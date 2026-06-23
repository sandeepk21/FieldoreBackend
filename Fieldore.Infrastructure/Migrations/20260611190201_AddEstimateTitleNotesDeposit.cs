using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateTitleNotesDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "deposit_amount",
                table: "estimates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "deposit_type",
                table: "estimates",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_value",
                table: "estimates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "internal_notes",
                table: "estimates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "estimates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deposit_amount",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "deposit_type",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "deposit_value",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "internal_notes",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "title",
                table: "estimates");
        }
    }
}
