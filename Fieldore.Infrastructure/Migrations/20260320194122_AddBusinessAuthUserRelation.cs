using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAuthUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "auth_user_id",
                table: "businesses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_businesses_auth_user_id",
                table: "businesses",
                column: "auth_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_businesses_auth_user_id",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "auth_user_id",
                table: "businesses");
        }
    }
}
