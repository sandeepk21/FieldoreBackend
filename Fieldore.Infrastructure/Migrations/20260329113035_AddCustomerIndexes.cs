using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "mobile_phone",
                table: "customers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "customers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "customers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_business_active",
                table: "customers",
                columns: new[] { "business_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_business_email",
                table: "customers",
                columns: new[] { "business_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_created_at",
                table: "customers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_customers_first_name",
                table: "customers",
                column: "first_name");

            migrationBuilder.CreateIndex(
                name: "ix_customers_last_name",
                table: "customers",
                column: "last_name");

            migrationBuilder.CreateIndex(
                name: "ux_customers_business_mobile",
                table: "customers",
                columns: new[] { "business_id", "mobile_phone" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_business_active",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_business_email",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_created_at",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_first_name",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_last_name",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ux_customers_business_mobile",
                table: "customers");

            migrationBuilder.AlterColumn<string>(
                name: "mobile_phone",
                table: "customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
