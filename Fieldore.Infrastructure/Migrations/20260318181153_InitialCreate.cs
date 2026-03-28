using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fieldore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password_salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "business_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    provider_subscription_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    plan_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    billing_cycle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    renews_on = table.Column<DateOnly>(type: "date", nullable: true),
                    trial_ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    trade_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    website_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    logo_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    company_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mobile_phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    alternate_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gate_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pets_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    internal_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_same_as_service = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "estimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    estimate_number = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: false),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    tax_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_name_snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customer_email_snapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    billing_longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    invoice_number = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    purchase_order_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    net_terms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: false),
                    due_on = table.Column<DateOnly>(type: "date", nullable: false),
                    tax_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    balance_due_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_name_snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customer_email_snapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    billing_latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    billing_longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_lead_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    job_number = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    job_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    scheduled_start_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    scheduled_end_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    actual_start_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    actual_end_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "int", nullable: true),
                    use_customer_primary_address = table.Column<bool>(type: "bit", nullable: false),
                    line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requested_service = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contacted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    converted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    push_enabled = table.Column<bool>(type: "bit", nullable: false),
                    email_enabled = table.Column<bool>(type: "bit", nullable: false),
                    sms_enabled = table.Column<bool>(type: "bit", nullable: false),
                    marketing_enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    auth_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    time_zone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "business_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_business_memberships_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_catalog_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    default_unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_catalog_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_catalog_items_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    is_billing = table.Column<bool>(type: "bit", nullable: false),
                    line1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    state_or_province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    postal_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_notes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estimate_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    estimate_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    service_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    line_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimate_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estimate_line_items_estimates_estimate_id",
                        column: x => x.estimate_id,
                        principalTable: "estimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    unit_rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    line_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    reference_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_records_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_assignments_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_checklist_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    task_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_completed = table.Column<bool>(type: "bit", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_checklist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_checklist_items_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_notes_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    storage_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    taken_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_photos_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_users_email",
                table: "auth_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_memberships_business_id_user_profile_id",
                table: "business_memberships",
                columns: new[] { "business_id", "user_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_subscriptions_business_id",
                table: "business_subscriptions",
                column: "business_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_id",
                table: "customer_addresses",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_notes_customer_id",
                table: "customer_notes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_estimate_line_items_estimate_id",
                table: "estimate_line_items",
                column: "estimate_id");

            migrationBuilder.CreateIndex(
                name: "IX_estimates_business_id_estimate_number",
                table: "estimates",
                columns: new[] { "business_id", "estimate_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_invoice_id",
                table: "invoice_line_items",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_business_id_invoice_number",
                table: "invoices",
                columns: new[] { "business_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_assignments_job_id_user_profile_id",
                table: "job_assignments",
                columns: new[] { "job_id", "user_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_checklist_items_job_id",
                table: "job_checklist_items",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_notes_job_id",
                table: "job_notes",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_photos_job_id",
                table: "job_photos",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_business_id_job_number",
                table: "jobs",
                columns: new[] { "business_id", "job_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_invoice_id",
                table: "payment_records",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_items_business_id",
                table: "service_catalog_items",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_preferences_user_profile_id",
                table: "user_notification_preferences",
                column: "user_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_auth_user_id",
                table: "user_profiles",
                column: "auth_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_users");

            migrationBuilder.DropTable(
                name: "business_memberships");

            migrationBuilder.DropTable(
                name: "business_subscriptions");

            migrationBuilder.DropTable(
                name: "customer_addresses");

            migrationBuilder.DropTable(
                name: "customer_notes");

            migrationBuilder.DropTable(
                name: "estimate_line_items");

            migrationBuilder.DropTable(
                name: "invoice_line_items");

            migrationBuilder.DropTable(
                name: "job_assignments");

            migrationBuilder.DropTable(
                name: "job_checklist_items");

            migrationBuilder.DropTable(
                name: "job_notes");

            migrationBuilder.DropTable(
                name: "job_photos");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "payment_records");

            migrationBuilder.DropTable(
                name: "service_catalog_items");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "estimates");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "businesses");
        }
    }
}
