using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeaddressfromuserprofiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "city",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "country",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "line1",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "line2",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "state_or_province",
                table: "user_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "user_profiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line1",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line2",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "user_profiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state_or_province",
                table: "user_profiles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
