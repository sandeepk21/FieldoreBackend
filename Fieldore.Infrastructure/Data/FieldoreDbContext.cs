using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace Fieldore.Infrastructure.Data;

public sealed class FieldoreDbContext : DbContext
{
    public FieldoreDbContext(DbContextOptions<FieldoreDbContext> options)
        : base(options)
    {
    }
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<AppUserProfile> UserProfiles => Set<AppUserProfile>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<BusinessSubscription> BusinessSubscriptions => Set<BusinessSubscription>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<StateProvince> StateProvinces => Set<StateProvince>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobLineItem> JobLineItems => Set<JobLineItem>();
    public DbSet<JobAssignment> JobAssignments => Set<JobAssignment>();
    public DbSet<JobChecklistItem> JobChecklistItems => Set<JobChecklistItem>();
    public DbSet<JobNote> JobNotes => Set<JobNote>();
    public DbSet<JobPhoto> JobPhotos => Set<JobPhoto>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateLineItem> EstimateLineItems => Set<EstimateLineItem>();
    public DbSet<EstimateAttachment> EstimateAttachments => Set<EstimateAttachment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property("Id").ValueGeneratedOnAdd();
            }
        }

        ConfigureAuth(modelBuilder);
        ConfigureUserProfiles(modelBuilder);
        ConfigureBusinesses(modelBuilder);
        ConfigureCustomers(modelBuilder);
        ConfigureLocations(modelBuilder);
        ConfigureLeads(modelBuilder);
        ConfigureJobs(modelBuilder);
        ConfigureEstimates(modelBuilder);
        ConfigureInvoices(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureExpenses(modelBuilder);
        ConfigureNotificationPreferences(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(x => x.CreatedAt).IsModified = false;
            }
        }
    }

    private static void ConfigureAuth(ModelBuilder modelBuilder)
    {
        var authUsers = modelBuilder.Entity<AuthUser>();
        authUsers.ToTable("auth_users");
        authUsers.HasKey(x => x.Id);
        authUsers.Property(x => x.Email).HasColumnName("email");
        authUsers.Property(x => x.PasswordHash).HasColumnName("password_hash");
        authUsers.Property(x => x.PasswordSalt).HasColumnName("password_salt");
        authUsers.Property(x => x.IsActive).HasColumnName("is_active");
        authUsers.HasIndex(x => x.Email).IsUnique();
        ConfigureAuditColumns(authUsers);
    }

    private static void ConfigureUserProfiles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppUserProfile>();

        entity.ToTable("user_profiles");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.AuthUserId).HasColumnName("auth_user_id");
        entity.Property(x => x.FirstName).HasColumnName("first_name");
        entity.Property(x => x.LastName).HasColumnName("last_name");
        entity.Property(x => x.DisplayName).HasColumnName("display_name");
        entity.Property(x => x.Email).HasColumnName("email");
        entity.Property(x => x.Phone).HasColumnName("phone");
        entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
        entity.Property(x => x.TimeZone).HasColumnName("time_zone");
        entity.Property(x => x.IsActive).HasColumnName("is_active");
        entity.HasIndex(x => x.AuthUserId).IsUnique();
        ConfigureAuditColumns(entity);
    }

    private static void ConfigureBusinesses(ModelBuilder modelBuilder)
    {
        var businesses = modelBuilder.Entity<Business>();
        businesses.ToTable("businesses");
        businesses.HasKey(x => x.Id);
        businesses.Property(x => x.AuthUserId).HasColumnName("auth_user_id");
        businesses.Property(x => x.Name).HasColumnName("name");
        businesses.Property(x => x.TradeType).HasColumnName("trade_type");
        businesses.Property(x => x.Email).HasColumnName("email");
        businesses.Property(x => x.Phone).HasColumnName("phone");
        businesses.Property(x => x.WebsiteUrl).HasColumnName("website_url");
        businesses.Property(x => x.LogoUrl).HasColumnName("logo_url");
        businesses.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8).HasDefaultValue("USD");
        businesses.Property(x => x.StripeAccountId).HasColumnName("stripe_account_id");
        businesses.Property(x => x.StripeOnboardingComplete).HasColumnName("stripe_onboarding_complete").HasDefaultValue(false);
        businesses.HasIndex(x => x.AuthUserId).IsUnique();
        ConfigureAddress(businesses, x => x.Address, string.Empty);
        ConfigureAuditColumns(businesses);

        var memberships = modelBuilder.Entity<BusinessMembership>();
        memberships.ToTable("business_memberships");
        memberships.HasKey(x => x.Id);
        memberships.Property(x => x.BusinessId).HasColumnName("business_id");
        memberships.Property(x => x.UserProfileId).HasColumnName("user_profile_id");
        memberships.Property(x => x.IsPrimary).HasColumnName("is_primary");
        memberships.Property(x => x.IsActive).HasColumnName("is_active");
        memberships.Property(x => x.Role).HasColumnName("role");
        memberships.HasIndex(x => new { x.BusinessId, x.UserProfileId }).IsUnique();
        ConfigureAuditColumns(memberships);

        var subscriptions = modelBuilder.Entity<BusinessSubscription>();
        subscriptions.ToTable("business_subscriptions");
        subscriptions.HasKey(x => x.Id);
        subscriptions.Property(x => x.BusinessId).HasColumnName("business_id");
        subscriptions.Property(x => x.Provider).HasColumnName("provider");
        subscriptions.Property(x => x.ProviderSubscriptionId).HasColumnName("provider_subscription_id");
        subscriptions.Property(x => x.PlanName).HasColumnName("plan_name");
        subscriptions.Property(x => x.BillingCycle).HasColumnName("billing_cycle");
        subscriptions.Property(x => x.RenewsOn).HasColumnName("renews_on");
        subscriptions.Property(x => x.TrialEndsOn).HasColumnName("trial_ends_on");
        subscriptions.Property(x => x.Status).HasColumnName("status");
        subscriptions.HasIndex(x => x.BusinessId).IsUnique();
        ConfigureAuditColumns(subscriptions);

        var services = modelBuilder.Entity<ServiceCatalogItem>();
        services.ToTable("service_catalog_items");
        services.HasKey(x => x.Id);
        services.Property(x => x.BusinessId).HasColumnName("business_id");
        services.Property(x => x.Name).HasColumnName("name");
        services.Property(x => x.Category).HasColumnName("category");
        services.Property(x => x.Description).HasColumnName("description");
        services.Property(x => x.DefaultUnitPrice).HasColumnName("default_unit_price");
        services.Property(x => x.IsActive).HasColumnName("is_active");
        ConfigureAuditColumns(services);
    }

    private static void ConfigureCustomers(ModelBuilder modelBuilder)
    {
        var customers = modelBuilder.Entity<Customer>();
        customers.ToTable("customers");
        customers.HasKey(x => x.Id);
        customers.Property(x => x.BusinessId).HasColumnName("business_id");
        customers.Property(x => x.Type).HasColumnName("type");
        customers.Property(x => x.CompanyName).HasColumnName("company_name");
        customers.Property(x => x.FirstName).HasColumnName("first_name");
        customers.Property(x => x.LastName).HasColumnName("last_name");
        customers.Property(x => x.Email).HasColumnName("email");
        customers.Property(x => x.MobilePhone).HasColumnName("mobile_phone");
        customers.Property(x => x.AlternatePhone).HasColumnName("alternate_phone");
        customers.Property(x => x.GateCode).HasColumnName("gate_code");
        customers.Property(x => x.PetsNote).HasColumnName("pets_note");
        customers.Property(x => x.InternalNotes).HasColumnName("internal_notes");
        customers.Property(x => x.BillingSameAsService).HasColumnName("billing_same_as_service");
        customers.Property(x => x.IsActive).HasColumnName("is_active");
        customers.HasIndex(x => new { x.BusinessId, x.MobilePhone })
            .IsUnique()
            .HasDatabaseName("ux_customers_business_mobile");
        customers.HasIndex(x => new { x.BusinessId, x.Email })
            .HasDatabaseName("ix_customers_business_email");
        customers.HasIndex(x => new { x.BusinessId, x.IsActive })
            .HasDatabaseName("ix_customers_business_active");

        customers.HasIndex(x => x.FirstName)
            .HasDatabaseName("ix_customers_first_name");

        customers.HasIndex(x => x.LastName)
            .HasDatabaseName("ix_customers_last_name");

        customers.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_customers_created_at");
        
        ConfigureAuditColumns(customers);

        var addresses = modelBuilder.Entity<CustomerAddress>();
        addresses.ToTable("customer_addresses");
        addresses.HasKey(x => x.Id);
        addresses.Property(x => x.CustomerId).HasColumnName("customer_id");
        addresses.Property(x => x.Label).HasColumnName("label");
        addresses.Property(x => x.IsPrimary).HasColumnName("is_primary");
        addresses.Property(x => x.IsBilling).HasColumnName("is_billing");
        ConfigureAddress(addresses, x => x.Address, string.Empty);
        ConfigureAuditColumns(addresses);

        var notes = modelBuilder.Entity<CustomerNote>();
        notes.ToTable("customer_notes");
        notes.HasKey(x => x.Id);
        notes.Property(x => x.CustomerId).HasColumnName("customer_id");
        notes.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        notes.Property(x => x.Body).HasColumnName("body");
        ConfigureAuditColumns(notes);
    }

    private static void ConfigureLeads(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Lead>();

        entity.ToTable("leads");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BusinessId).HasColumnName("business_id");
        entity.Property(x => x.CustomerId).HasColumnName("customer_id");
        entity.Property(x => x.FirstName).HasColumnName("first_name");
        entity.Property(x => x.LastName).HasColumnName("last_name");
        entity.Property(x => x.Email).HasColumnName("email");
        entity.Property(x => x.Phone).HasColumnName("phone");
        entity.Property(x => x.RequestedService).HasColumnName("requested_service");
        entity.Property(x => x.Source).HasColumnName("source");
        entity.Property(x => x.Status).HasColumnName("status");
        entity.Property(x => x.Notes).HasColumnName("notes");
        entity.Property(x => x.ContactedAt).HasColumnName("contacted_at");
        entity.Property(x => x.ConvertedAt).HasColumnName("converted_at");
        ConfigureAuditColumns(entity);
    }

    private static void ConfigureLocations(ModelBuilder modelBuilder)
    {
        var countries = modelBuilder.Entity<Country>();
        countries.ToTable("countries");
        countries.HasKey(x => x.Id);
        countries.Property(x => x.Name).HasColumnName("name");
        countries.Property(x => x.Code).HasColumnName("code");
        countries.HasIndex(x => x.Code).IsUnique();
        countries.HasIndex(x => x.Name).IsUnique();
        countries.HasData(LocationSeedData.Countries);
        ConfigureAuditColumns(countries);

        var states = modelBuilder.Entity<StateProvince>();
        states.ToTable("state_provinces");
        states.HasKey(x => x.Id);
        states.Property(x => x.CountryId).HasColumnName("country_id");
        states.Property(x => x.Name).HasColumnName("name");
        states.Property(x => x.Code).HasColumnName("code");
        states.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();
        states.HasIndex(x => new { x.CountryId, x.Name }).IsUnique();
        states
            .HasOne(x => x.Country)
            .WithMany(x => x.States)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
        states.HasData(LocationSeedData.States);
        ConfigureAuditColumns(states);
    }

    private static void ConfigureJobs(ModelBuilder modelBuilder)
    {
        var jobs = modelBuilder.Entity<Job>();
        jobs.ToTable("jobs");
        jobs.HasKey(x => x.Id);
        jobs.Property(x => x.BusinessId).HasColumnName("business_id");
        jobs.Property(x => x.CustomerId).HasColumnName("customer_id");
        jobs.Property(x => x.SourceLeadId).HasColumnName("source_lead_id");
        jobs.Property(x => x.SourceEstimateId).HasColumnName("source_estimate_id");
        jobs.Property(x => x.JobNumber).HasColumnName("job_number");
        jobs.Property(x => x.Title).HasColumnName("title");
        jobs.Property(x => x.JobType).HasColumnName("job_type");
        jobs.Property(x => x.Priority).HasColumnName("priority");
        jobs.Property(x => x.Status).HasColumnName("status");
        jobs.Property(x => x.ScheduledStartAt).HasColumnName("scheduled_start_at");
        jobs.Property(x => x.ScheduledEndAt).HasColumnName("scheduled_end_at");
        jobs.Property(x => x.ActualStartAt).HasColumnName("actual_start_at");
        jobs.Property(x => x.ActualEndAt).HasColumnName("actual_end_at");
        jobs.Property(x => x.EstimatedDurationMinutes).HasColumnName("estimated_duration_minutes");
        jobs.Property(x => x.UseCustomerPrimaryAddress).HasColumnName("use_customer_primary_address");
        jobs.Property(x => x.Description).HasColumnName("description");
        jobs.HasIndex(x => new { x.BusinessId, x.JobNumber }).IsUnique();
        ConfigureAddress(jobs, x => x.ServiceAddress, string.Empty);
        ConfigureAuditColumns(jobs);

        var lineItems = modelBuilder.Entity<JobLineItem>();
        lineItems.ToTable("job_line_items");
        lineItems.HasKey(x => x.Id);
        lineItems.Property(x => x.JobId).HasColumnName("job_id");
        lineItems.Property(x => x.SortOrder).HasColumnName("sort_order");
        lineItems.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(200);
        lineItems.Property(x => x.Description).HasColumnName("description");
        lineItems.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("decimal(18,4)");
        lineItems.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
        lineItems.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("decimal(18,2)");
        ConfigureAuditColumns(lineItems);

        var assignments = modelBuilder.Entity<JobAssignment>();
        assignments.ToTable("job_assignments");
        assignments.HasKey(x => x.Id);
        assignments.Property(x => x.JobId).HasColumnName("job_id");
        assignments.Property(x => x.UserProfileId).HasColumnName("user_profile_id");
        assignments.Property(x => x.IsPrimary).HasColumnName("is_primary");
        assignments.HasIndex(x => new { x.JobId, x.UserProfileId }).IsUnique();
        ConfigureAuditColumns(assignments);

        var checklist = modelBuilder.Entity<JobChecklistItem>();
        checklist.ToTable("job_checklist_items");
        checklist.HasKey(x => x.Id);
        checklist.Property(x => x.JobId).HasColumnName("job_id");
        checklist.Property(x => x.SortOrder).HasColumnName("sort_order");
        checklist.Property(x => x.TaskName).HasColumnName("task_name");
        checklist.Property(x => x.IsCompleted).HasColumnName("is_completed");
        checklist.Property(x => x.CompletedAt).HasColumnName("completed_at");
        checklist.Property(x => x.CompletedByUserId).HasColumnName("completed_by_user_id");
        ConfigureAuditColumns(checklist);

        var notes = modelBuilder.Entity<JobNote>();
        notes.ToTable("job_notes");
        notes.HasKey(x => x.Id);
        notes.Property(x => x.JobId).HasColumnName("job_id");
        notes.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        notes.Property(x => x.Body).HasColumnName("body");
        ConfigureAuditColumns(notes);

        var photos = modelBuilder.Entity<JobPhoto>();
        photos.ToTable("job_photos");
        photos.HasKey(x => x.Id);
        photos.Property(x => x.JobId).HasColumnName("job_id");
        photos.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");
        photos.Property(x => x.StoragePath).HasColumnName("storage_path");
        photos.Property(x => x.Caption).HasColumnName("caption");
        photos.Property(x => x.TakenAt).HasColumnName("taken_at");
        ConfigureAuditColumns(photos);
    }

    private static void ConfigureEstimates(ModelBuilder modelBuilder)
    {
        var estimates = modelBuilder.Entity<Estimate>();
        estimates.ToTable("estimates");
        estimates.HasKey(x => x.Id);
        estimates.Property(x => x.BusinessId).HasColumnName("business_id");
        estimates.Property(x => x.CustomerId).HasColumnName("customer_id");
        estimates.Property(x => x.EstimateNumber).HasColumnName("estimate_number");
        estimates.Property(x => x.Status).HasColumnName("status");
        estimates.Property(x => x.IssuedOn).HasColumnName("issued_on");
        estimates.Property(x => x.ExpiresOn).HasColumnName("expires_on");
        estimates.Property(x => x.TaxRate).HasColumnName("tax_rate");
        estimates.Property(x => x.DiscountAmount).HasColumnName("discount_amount");
        estimates.Property(x => x.SubtotalAmount).HasColumnName("subtotal_amount");
        estimates.Property(x => x.TaxAmount).HasColumnName("tax_amount");
        estimates.Property(x => x.TotalAmount).HasColumnName("total_amount");
        estimates.Property(x => x.DepositType).HasColumnName("deposit_type").HasMaxLength(16).HasDefaultValue(EstimateDepositTypes.None);
        estimates.Property(x => x.DepositValue).HasColumnName("deposit_value").HasDefaultValue(0m);
        estimates.Property(x => x.DepositAmount).HasColumnName("deposit_amount").HasDefaultValue(0m);
        estimates.Property(x => x.Title).HasColumnName("title");
        estimates.Property(x => x.Notes).HasColumnName("notes");
        estimates.Property(x => x.InternalNotes).HasColumnName("internal_notes");
        estimates.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot");
        estimates.Property(x => x.CustomerEmailSnapshot).HasColumnName("customer_email_snapshot");
        estimates.Property(x => x.PublicToken).HasColumnName("public_token");
        estimates.Property(x => x.SentAt).HasColumnName("sent_at");
        estimates.Property(x => x.RespondedAt).HasColumnName("responded_at");
        estimates.Property(x => x.ConvertedJobId).HasColumnName("converted_job_id");
        estimates.HasIndex(x => new { x.BusinessId, x.EstimateNumber }).IsUnique();
        estimates.HasIndex(x => x.PublicToken).IsUnique().HasFilter("[public_token] IS NOT NULL");
        ConfigureAddress(estimates, x => x.BillingAddressSnapshot, "billing_");
        ConfigureAuditColumns(estimates);

        var lines = modelBuilder.Entity<EstimateLineItem>();
        lines.ToTable("estimate_line_items");
        lines.HasKey(x => x.Id);
        lines.Property(x => x.EstimateId).HasColumnName("estimate_id");
        lines.Property(x => x.SortOrder).HasColumnName("sort_order");
        lines.Property(x => x.ServiceName).HasColumnName("service_name");
        lines.Property(x => x.Description).HasColumnName("description");
        lines.Property(x => x.Quantity).HasColumnName("quantity");
        lines.Property(x => x.UnitPrice).HasColumnName("unit_price");
        lines.Property(x => x.LineTotal).HasColumnName("line_total");
        ConfigureAuditColumns(lines);

        var attachments = modelBuilder.Entity<EstimateAttachment>();
        attachments.ToTable("estimate_attachments");
        attachments.HasKey(x => x.Id);
        attachments.Property(x => x.EstimateId).HasColumnName("estimate_id");
        attachments.Property(x => x.FileName).HasColumnName("file_name");
        attachments.Property(x => x.StoragePath).HasColumnName("storage_path");
        attachments.Property(x => x.ContentType).HasColumnName("content_type");
        attachments.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
        attachments.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");
        attachments.HasIndex(x => x.EstimateId).HasDatabaseName("ix_estimate_attachments_estimate");
        ConfigureAuditColumns(attachments);
    }

    private static void ConfigureInvoices(ModelBuilder modelBuilder)
    {
        var invoices = modelBuilder.Entity<Invoice>();
        invoices.ToTable("invoices");
        invoices.HasKey(x => x.Id);
        invoices.Property(x => x.BusinessId).HasColumnName("business_id");
        invoices.Property(x => x.CustomerId).HasColumnName("customer_id");
        invoices.Property(x => x.JobId).HasColumnName("job_id");
        invoices.Property(x => x.InvoiceNumber).HasColumnName("invoice_number");
        invoices.Property(x => x.PurchaseOrderNumber).HasColumnName("purchase_order_number");
        invoices.Property(x => x.NetTerms).HasColumnName("net_terms");
        invoices.Property(x => x.Status).HasColumnName("status");
        invoices.Property(x => x.IssuedOn).HasColumnName("issued_on");
        invoices.Property(x => x.DueOn).HasColumnName("due_on");
        invoices.Property(x => x.TaxRate).HasColumnName("tax_rate");
        invoices.Property(x => x.DiscountAmount).HasColumnName("discount_amount");
        invoices.Property(x => x.SubtotalAmount).HasColumnName("subtotal_amount");
        invoices.Property(x => x.TaxAmount).HasColumnName("tax_amount");
        invoices.Property(x => x.TotalAmount).HasColumnName("total_amount");
        invoices.Property(x => x.BalanceDueAmount).HasColumnName("balance_due_amount");
        invoices.Property(x => x.Notes).HasColumnName("notes");
        invoices.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot");
        invoices.Property(x => x.CustomerEmailSnapshot).HasColumnName("customer_email_snapshot");
        invoices.HasIndex(x => new { x.BusinessId, x.InvoiceNumber }).IsUnique();
        ConfigureAddress(invoices, x => x.BillingAddressSnapshot, "billing_");
        ConfigureAuditColumns(invoices);

        var lines = modelBuilder.Entity<InvoiceLineItem>();
        lines.ToTable("invoice_line_items");
        lines.HasKey(x => x.Id);
        lines.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        lines.Property(x => x.SortOrder).HasColumnName("sort_order");
        lines.Property(x => x.Name).HasColumnName("name");
        lines.Property(x => x.Description).HasColumnName("description");
        lines.Property(x => x.Quantity).HasColumnName("quantity");
        lines.Property(x => x.UnitRate).HasColumnName("unit_rate");
        lines.Property(x => x.LineTotal).HasColumnName("line_total");
        ConfigureAuditColumns(lines);
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PaymentRecord>();

        entity.ToTable("payment_records");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        entity.Property(x => x.Amount).HasColumnName("amount");
        entity.Property(x => x.Method).HasColumnName("method");
        entity.Property(x => x.PaidAt).HasColumnName("paid_at");
        entity.Property(x => x.ReferenceNumber).HasColumnName("reference_number");
        entity.Property(x => x.Notes).HasColumnName("notes");
        entity.Property(x => x.RecordedByUserId).HasColumnName("recorded_by_user_id");
        ConfigureAuditColumns(entity);
    }

    private static void ConfigureExpenses(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Expense>();
        entity.ToTable("expenses");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BusinessId).HasColumnName("business_id");
        entity.Property(x => x.JobId).HasColumnName("job_id");
        entity.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        entity.Property(x => x.Category).HasColumnName("category").HasMaxLength(50);
        entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
        entity.Property(x => x.ExpenseDate).HasColumnName("expense_date");
        entity.Property(x => x.VendorName).HasColumnName("vendor_name").HasMaxLength(200);
        entity.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        entity.Property(x => x.Notes).HasColumnName("notes");
        ConfigureAuditColumns(entity);
    }

    private static void ConfigureNotificationPreferences(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserNotificationPreference>();

        entity.ToTable("user_notification_preferences");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.UserProfileId).HasColumnName("user_profile_id");
        entity.Property(x => x.PushEnabled).HasColumnName("push_enabled");
        entity.Property(x => x.EmailEnabled).HasColumnName("email_enabled");
        entity.Property(x => x.SmsEnabled).HasColumnName("sms_enabled");
        entity.Property(x => x.MarketingEnabled).HasColumnName("marketing_enabled");
        entity.HasIndex(x => x.UserProfileId).IsUnique();
        ConfigureAuditColumns(entity);
    }

    private static void ConfigureAddress<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, Address?>> navigationExpression,
        string prefix)
        where TEntity : class
    {
        entity.OwnsOne(navigationExpression, address =>
        {
            address.Property(x => x.Line1).HasColumnName($"{prefix}line1");
            address.Property(x => x.Line2).HasColumnName($"{prefix}line2");
            address.Property(x => x.City).HasColumnName($"{prefix}city");
            address.Property(x => x.StateOrProvince).HasColumnName($"{prefix}state_or_province");
            address.Property(x => x.PostalCode).HasColumnName($"{prefix}postal_code");
            address.Property(x => x.Country).HasColumnName($"{prefix}country");
            address.Property(x => x.Latitude).HasColumnName($"{prefix}latitude");
            address.Property(x => x.Longitude).HasColumnName($"{prefix}longitude");
        });
    }

    private static void ConfigureAuditColumns<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        entity.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        entity.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
    }
}
