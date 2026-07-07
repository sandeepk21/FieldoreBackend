using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "cancel_at_period_end",
                table: "business_subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "business_subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "current_period_end",
                table: "business_subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "current_period_start",
                table: "business_subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ended_at",
                table: "business_subscriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "plan_id",
                table: "business_subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "plan_price_id",
                table: "business_subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "business_subscriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "business_subscriptions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_platform_admin",
                table: "auth_users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "billing_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    stripe_event_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "received"),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    percent_off = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    amount_off = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "USD"),
                    max_redemptions = table.Column<int>(type: "int", nullable: true),
                    redeem_by = table.Column<DateOnly>(type: "date", nullable: true),
                    stripe_coupon_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "USD"),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_recommended = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    visibility = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "public"),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    badge = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    button_text = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "Get Started"),
                    color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    trial_days = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_jobs_count = table.Column<int>(type: "int", nullable: false),
                    invoices_created_count = table.Column<int>(type: "int", nullable: false),
                    customers_added_count = table.Column<int>(type: "int", nullable: false),
                    employees_count = table.Column<int>(type: "int", nullable: false),
                    storage_used_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_usages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plan_features",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    feature_key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    limit_value = table.Column<int>(type: "int", nullable: true),
                    display_label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    show_on_pricing = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_features_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    plan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    billing_cycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "USD"),
                    stripe_price_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_prices_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_plan_id",
                table: "business_subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_stripe_subscription_id",
                table: "business_subscriptions",
                column: "stripe_subscription_id",
                filter: "[stripe_subscription_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_billing_events_stripe_event_id",
                table: "billing_events",
                column: "stripe_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_features_plan_id_feature_key",
                table: "plan_features",
                columns: new[] { "plan_id", "feature_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_prices_plan_id_billing_cycle",
                table: "plan_prices",
                columns: new[] { "plan_id", "billing_cycle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_slug",
                table: "subscription_plans",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_usages_business_id_period_start",
                table: "subscription_usages",
                columns: new[] { "business_id", "period_start" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_business_subscriptions_subscription_plans_plan_id",
                table: "business_subscriptions",
                column: "plan_id",
                principalTable: "subscription_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_business_subscriptions_subscription_plans_plan_id",
                table: "business_subscriptions");

            migrationBuilder.DropTable(
                name: "billing_events");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "plan_features");

            migrationBuilder.DropTable(
                name: "plan_prices");

            migrationBuilder.DropTable(
                name: "subscription_usages");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_business_subscriptions_plan_id",
                table: "business_subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_business_subscriptions_stripe_subscription_id",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "cancel_at_period_end",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "current_period_end",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "current_period_start",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "ended_at",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "plan_id",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "plan_price_id",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "business_subscriptions");

            migrationBuilder.DropColumn(
                name: "is_platform_admin",
                table: "auth_users");
        }
    }
}
