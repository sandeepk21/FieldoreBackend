# Fieldore Supabase Schema Notes

This schema was derived from the current React Native screens in `fieldore/app`.

## Screen-to-entity mapping

- Business setup and settings:
  - `businesses`
  - `business_memberships`
  - `business_subscriptions`
  - `service_catalog_items`
  - `user_notification_preferences`
- Auth and account profile:
  - `user_profiles`
  - Supabase Auth remains the source of truth for password and 2FA
- Customers:
  - `customers`
  - `customer_addresses`
  - `customer_notes`
- Leads:
  - `leads`
- Jobs and calendar:
  - `jobs`
  - `job_assignments`
  - `job_checklist_items`
  - `job_notes`
  - `job_photos`
- Estimates:
  - `estimates`
  - `estimate_line_items`
- Invoices and payments:
  - `invoices`
  - `invoice_line_items`
  - `payment_records`

## Key design choices

- UUID primary keys align well with Supabase and mobile-first syncing.
- `business_id` is used as the tenant boundary for most records.
- Snapshot fields are stored on invoices and estimates so customer billing history does not change when a customer record is edited later.
- Job addresses are stored directly on `jobs` so scheduled work keeps its original service location even if the customer address changes.
- Basic RLS policies are included so business members only access their own tenant data.

## Apply in Supabase

Run `FieldoreBackend/database/supabase_schema.sql` in the Supabase SQL editor or through your migration workflow.
