using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatePublicWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "converted_job_id",
                table: "estimates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "public_token",
                table: "estimates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "responded_at",
                table: "estimates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "estimates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimates_public_token",
                table: "estimates",
                column: "public_token",
                unique: true,
                filter: "[public_token] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_estimates_public_token",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "converted_job_id",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "public_token",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "responded_at",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "estimates");
        }
    }
}
