create extension if not exists pgcrypto;

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = timezone('utc', now());
  return new;
end;
$$;

create table public.auth_users (
  id uuid primary key default gen_random_uuid(),
  email text not null unique,
  password_hash text not null,
  password_salt text not null,
  is_active boolean not null default true,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.user_profiles (
  id uuid primary key default gen_random_uuid(),
  auth_user_id uuid not null unique references public.auth_users (id) on delete cascade,
  first_name text not null,
  last_name text not null,
  display_name text,
  email text,
  phone text,
  avatar_url text,
  time_zone text,
  address_line1 text,
  address_line2 text,
  city text,
  state_or_province text,
  postal_code text,
  country text,
  latitude numeric(9, 6),
  longitude numeric(9, 6),
  is_active boolean not null default true,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.businesses (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  trade_type text,
  email text,
  phone text,
  website_url text,
  logo_url text,
  address_line1 text,
  address_line2 text,
  city text,
  state_or_province text,
  postal_code text,
  country text,
  latitude numeric(9, 6),
  longitude numeric(9, 6),
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.business_memberships (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  user_profile_id uuid not null references public.user_profiles (id) on delete cascade,
  role text not null default 'staff',
  is_primary boolean not null default false,
  is_active boolean not null default true,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  unique (business_id, user_profile_id)
);

create table public.business_subscriptions (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null unique references public.businesses (id) on delete cascade,
  provider text not null default 'supabase',
  provider_subscription_id text,
  plan_name text not null,
  billing_cycle text not null default 'monthly',
  status text not null default 'trial',
  renews_on date,
  trial_ends_on date,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.service_catalog_items (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  name text not null,
  category text,
  description text,
  default_unit_price numeric(12, 2),
  is_active boolean not null default true,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.customers (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  type text not null,
  company_name text,
  first_name text not null,
  last_name text not null,
  email text,
  mobile_phone text not null,
  alternate_phone text,
  gate_code text,
  pets_note text,
  internal_notes text,
  billing_same_as_service boolean not null default true,
  is_active boolean not null default true,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.customer_addresses (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid not null references public.customers (id) on delete cascade,
  label text not null default 'Service',
  is_primary boolean not null default false,
  is_billing boolean not null default false,
  line1 text,
  line2 text,
  city text,
  state_or_province text,
  postal_code text,
  country text,
  latitude numeric(9, 6),
  longitude numeric(9, 6),
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.customer_notes (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid not null references public.customers (id) on delete cascade,
  created_by_user_id uuid references public.user_profiles (id) on delete set null,
  body text not null,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.leads (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  customer_id uuid references public.customers (id) on delete set null,
  first_name text not null,
  last_name text,
  email text,
  phone text,
  requested_service text not null,
  source text not null,
  status text not null default 'new',
  notes text,
  contacted_at timestamptz,
  converted_at timestamptz,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.jobs (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  customer_id uuid not null references public.customers (id) on delete restrict,
  source_lead_id uuid references public.leads (id) on delete set null,
  job_number text not null,
  title text not null,
  job_type text,
  priority text not null default 'normal',
  status text not null default 'draft',
  scheduled_start_at timestamptz not null,
  scheduled_end_at timestamptz,
  actual_start_at timestamptz,
  actual_end_at timestamptz,
  estimated_duration_minutes integer,
  use_customer_primary_address boolean not null default true,
  address_line1 text,
  address_line2 text,
  city text,
  state_or_province text,
  postal_code text,
  country text,
  latitude numeric(9, 6),
  longitude numeric(9, 6),
  description text,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  unique (business_id, job_number)
);

create table public.job_assignments (
  id uuid primary key default gen_random_uuid(),
  job_id uuid not null references public.jobs (id) on delete cascade,
  user_profile_id uuid not null references public.user_profiles (id) on delete cascade,
  is_primary boolean not null default false,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  unique (job_id, user_profile_id)
);

create table public.job_checklist_items (
  id uuid primary key default gen_random_uuid(),
  job_id uuid not null references public.jobs (id) on delete cascade,
  sort_order integer not null default 0,
  task_name text not null,
  is_completed boolean not null default false,
  completed_at timestamptz,
  completed_by_user_id uuid references public.user_profiles (id) on delete set null,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.job_notes (
  id uuid primary key default gen_random_uuid(),
  job_id uuid not null references public.jobs (id) on delete cascade,
  created_by_user_id uuid references public.user_profiles (id) on delete set null,
  body text not null,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.job_photos (
  id uuid primary key default gen_random_uuid(),
  job_id uuid not null references public.jobs (id) on delete cascade,
  uploaded_by_user_id uuid references public.user_profiles (id) on delete set null,
  storage_path text not null,
  caption text,
  taken_at timestamptz,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.estimates (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  customer_id uuid not null references public.customers (id) on delete restrict,
  estimate_number text not null,
  status text not null default 'draft',
  issued_on date not null default current_date,
  expires_on date,
  tax_rate numeric(5, 2) not null default 0,
  discount_amount numeric(12, 2) not null default 0,
  subtotal_amount numeric(12, 2) not null default 0,
  tax_amount numeric(12, 2) not null default 0,
  total_amount numeric(12, 2) not null default 0,
  notes text,
  customer_name_snapshot text not null,
  customer_email_snapshot text,
  billing_line1 text,
  billing_line2 text,
  billing_city text,
  billing_state_or_province text,
  billing_postal_code text,
  billing_country text,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  unique (business_id, estimate_number)
);

create table public.estimate_line_items (
  id uuid primary key default gen_random_uuid(),
  estimate_id uuid not null references public.estimates (id) on delete cascade,
  sort_order integer not null default 0,
  service_name text not null,
  description text,
  quantity numeric(12, 2) not null default 1,
  unit_price numeric(12, 2) not null default 0,
  line_total numeric(12, 2) not null default 0,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.invoices (
  id uuid primary key default gen_random_uuid(),
  business_id uuid not null references public.businesses (id) on delete cascade,
  customer_id uuid not null references public.customers (id) on delete restrict,
  job_id uuid references public.jobs (id) on delete set null,
  invoice_number text not null,
  purchase_order_number text,
  net_terms text not null default 'Net 30',
  status text not null default 'draft',
  issued_on date not null default current_date,
  due_on date not null,
  tax_rate numeric(5, 2) not null default 0,
  discount_amount numeric(12, 2) not null default 0,
  subtotal_amount numeric(12, 2) not null default 0,
  tax_amount numeric(12, 2) not null default 0,
  total_amount numeric(12, 2) not null default 0,
  balance_due_amount numeric(12, 2) not null default 0,
  notes text,
  customer_name_snapshot text not null,
  customer_email_snapshot text,
  billing_line1 text,
  billing_line2 text,
  billing_city text,
  billing_state_or_province text,
  billing_postal_code text,
  billing_country text,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  unique (business_id, invoice_number)
);

create table public.invoice_line_items (
  id uuid primary key default gen_random_uuid(),
  invoice_id uuid not null references public.invoices (id) on delete cascade,
  sort_order integer not null default 0,
  name text not null,
  description text,
  quantity numeric(12, 2) not null default 1,
  unit_rate numeric(12, 2) not null default 0,
  line_total numeric(12, 2) not null default 0,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.payment_records (
  id uuid primary key default gen_random_uuid(),
  invoice_id uuid not null references public.invoices (id) on delete cascade,
  amount numeric(12, 2) not null,
  method text not null,
  paid_at timestamptz not null default timezone('utc', now()),
  reference_number text,
  notes text,
  recorded_by_user_id uuid references public.user_profiles (id) on delete set null,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create table public.user_notification_preferences (
  id uuid primary key default gen_random_uuid(),
  user_profile_id uuid not null unique references public.user_profiles (id) on delete cascade,
  push_enabled boolean not null default true,
  email_enabled boolean not null default true,
  sms_enabled boolean not null default false,
  marketing_enabled boolean not null default false,
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now())
);

create index idx_business_memberships_user_profile_id on public.business_memberships (user_profile_id);
create index idx_customers_business_id on public.customers (business_id);
create index idx_customer_addresses_customer_id on public.customer_addresses (customer_id);
create index idx_leads_business_id_status on public.leads (business_id, status);
create index idx_jobs_business_id_status_start on public.jobs (business_id, status, scheduled_start_at);
create index idx_job_assignments_user_profile_id on public.job_assignments (user_profile_id);
create index idx_estimates_business_id_status on public.estimates (business_id, status);
create index idx_invoices_business_id_status_due_on on public.invoices (business_id, status, due_on);
create index idx_payment_records_invoice_id on public.payment_records (invoice_id);

create or replace function public.is_business_member(target_business_id uuid)
returns boolean
language sql
stable
as $$
  select exists (
    select 1
    from public.business_memberships membership
    join public.user_profiles profile on profile.id = membership.user_profile_id
    where membership.business_id = target_business_id
      and membership.is_active = true
      and profile.auth_user_id = auth.uid()
  );
$$;

alter table public.user_profiles enable row level security;
alter table public.businesses enable row level security;
alter table public.business_memberships enable row level security;
alter table public.business_subscriptions enable row level security;
alter table public.service_catalog_items enable row level security;
alter table public.customers enable row level security;
alter table public.customer_addresses enable row level security;
alter table public.customer_notes enable row level security;
alter table public.leads enable row level security;
alter table public.jobs enable row level security;
alter table public.job_assignments enable row level security;
alter table public.job_checklist_items enable row level security;
alter table public.job_notes enable row level security;
alter table public.job_photos enable row level security;
alter table public.estimates enable row level security;
alter table public.estimate_line_items enable row level security;
alter table public.invoices enable row level security;
alter table public.invoice_line_items enable row level security;
alter table public.payment_records enable row level security;
alter table public.user_notification_preferences enable row level security;

create policy "user_profiles_select_own" on public.user_profiles
for select using (auth_user_id = auth.uid());

create policy "user_profiles_update_own" on public.user_profiles
for update using (auth_user_id = auth.uid());

create policy "businesses_member_access" on public.businesses
for all using (public.is_business_member(id))
with check (public.is_business_member(id));

create policy "memberships_member_access" on public.business_memberships
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "subscriptions_member_access" on public.business_subscriptions
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "services_member_access" on public.service_catalog_items
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "customers_member_access" on public.customers
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "customer_addresses_member_access" on public.customer_addresses
for all using (
  exists (
    select 1
    from public.customers customer
    where customer.id = customer_addresses.customer_id
      and public.is_business_member(customer.business_id)
  )
)
with check (
  exists (
    select 1
    from public.customers customer
    where customer.id = customer_addresses.customer_id
      and public.is_business_member(customer.business_id)
  )
);

create policy "customer_notes_member_access" on public.customer_notes
for all using (
  exists (
    select 1
    from public.customers customer
    where customer.id = customer_notes.customer_id
      and public.is_business_member(customer.business_id)
  )
)
with check (
  exists (
    select 1
    from public.customers customer
    where customer.id = customer_notes.customer_id
      and public.is_business_member(customer.business_id)
  )
);

create policy "leads_member_access" on public.leads
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "jobs_member_access" on public.jobs
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "job_assignments_member_access" on public.job_assignments
for all using (
  exists (
    select 1
    from public.jobs job
    where job.id = job_assignments.job_id
      and public.is_business_member(job.business_id)
  )
)
with check (
  exists (
    select 1
    from public.jobs job
    where job.id = job_assignments.job_id
      and public.is_business_member(job.business_id)
  )
);

create policy "job_checklist_member_access" on public.job_checklist_items
for all using (
  exists (
    select 1
    from public.jobs job
    where job.id = job_checklist_items.job_id
      and public.is_business_member(job.business_id)
  )
)
with check (
  exists (
    select 1
    from public.jobs job
    where job.id = job_checklist_items.job_id
      and public.is_business_member(job.business_id)
  )
);

create policy "job_notes_member_access" on public.job_notes
for all using (
  exists (
    select 1
    from public.jobs job
    where job.id = job_notes.job_id
      and public.is_business_member(job.business_id)
  )
)
with check (
  exists (
    select 1
    from public.jobs job
    where job.id = job_notes.job_id
      and public.is_business_member(job.business_id)
  )
);

create policy "job_photos_member_access" on public.job_photos
for all using (
  exists (
    select 1
    from public.jobs job
    where job.id = job_photos.job_id
      and public.is_business_member(job.business_id)
  )
)
with check (
  exists (
    select 1
    from public.jobs job
    where job.id = job_photos.job_id
      and public.is_business_member(job.business_id)
  )
);

create policy "estimates_member_access" on public.estimates
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "estimate_line_items_member_access" on public.estimate_line_items
for all using (
  exists (
    select 1
    from public.estimates estimate
    where estimate.id = estimate_line_items.estimate_id
      and public.is_business_member(estimate.business_id)
  )
)
with check (
  exists (
    select 1
    from public.estimates estimate
    where estimate.id = estimate_line_items.estimate_id
      and public.is_business_member(estimate.business_id)
  )
);

create policy "invoices_member_access" on public.invoices
for all using (public.is_business_member(business_id))
with check (public.is_business_member(business_id));

create policy "invoice_line_items_member_access" on public.invoice_line_items
for all using (
  exists (
    select 1
    from public.invoices invoice
    where invoice.id = invoice_line_items.invoice_id
      and public.is_business_member(invoice.business_id)
  )
)
with check (
  exists (
    select 1
    from public.invoices invoice
    where invoice.id = invoice_line_items.invoice_id
      and public.is_business_member(invoice.business_id)
  )
);

create policy "payment_records_member_access" on public.payment_records
for all using (
  exists (
    select 1
    from public.invoices invoice
    where invoice.id = payment_records.invoice_id
      and public.is_business_member(invoice.business_id)
  )
)
with check (
  exists (
    select 1
    from public.invoices invoice
    where invoice.id = payment_records.invoice_id
      and public.is_business_member(invoice.business_id)
  )
);

create policy "notification_preferences_own" on public.user_notification_preferences
for all using (
  exists (
    select 1
    from public.user_profiles profile
    where profile.id = user_notification_preferences.user_profile_id
      and profile.auth_user_id = auth.uid()
  )
)
with check (
  exists (
    select 1
    from public.user_profiles profile
    where profile.id = user_notification_preferences.user_profile_id
      and profile.auth_user_id = auth.uid()
  )
);

create trigger set_user_profiles_updated_at
before update on public.user_profiles
for each row execute function public.set_updated_at();

create trigger set_businesses_updated_at
before update on public.businesses
for each row execute function public.set_updated_at();

create trigger set_business_memberships_updated_at
before update on public.business_memberships
for each row execute function public.set_updated_at();

create trigger set_business_subscriptions_updated_at
before update on public.business_subscriptions
for each row execute function public.set_updated_at();

create trigger set_service_catalog_items_updated_at
before update on public.service_catalog_items
for each row execute function public.set_updated_at();

create trigger set_customers_updated_at
before update on public.customers
for each row execute function public.set_updated_at();

create trigger set_customer_addresses_updated_at
before update on public.customer_addresses
for each row execute function public.set_updated_at();

create trigger set_customer_notes_updated_at
before update on public.customer_notes
for each row execute function public.set_updated_at();

create trigger set_leads_updated_at
before update on public.leads
for each row execute function public.set_updated_at();

create trigger set_jobs_updated_at
before update on public.jobs
for each row execute function public.set_updated_at();

create trigger set_job_assignments_updated_at
before update on public.job_assignments
for each row execute function public.set_updated_at();

create trigger set_job_checklist_items_updated_at
before update on public.job_checklist_items
for each row execute function public.set_updated_at();

create trigger set_job_notes_updated_at
before update on public.job_notes
for each row execute function public.set_updated_at();

create trigger set_job_photos_updated_at
before update on public.job_photos
for each row execute function public.set_updated_at();

create trigger set_estimates_updated_at
before update on public.estimates
for each row execute function public.set_updated_at();

create trigger set_estimate_line_items_updated_at
before update on public.estimate_line_items
for each row execute function public.set_updated_at();

create trigger set_invoices_updated_at
before update on public.invoices
for each row execute function public.set_updated_at();

create trigger set_invoice_line_items_updated_at
before update on public.invoice_line_items
for each row execute function public.set_updated_at();

create trigger set_payment_records_updated_at
before update on public.payment_records
for each row execute function public.set_updated_at();

create trigger set_user_notification_preferences_updated_at
before update on public.user_notification_preferences
for each row execute function public.set_updated_at();
