# Architecture

## Overview

Apartment Management System is a single ASP.NET Core MVC application. It uses a feature-oriented application layer above shared infrastructure and a SQL Server database. Browser requests are served by Razor views; there is no separate public API or client application.

```text
Browser
  -> MVC controller + Razor view
  -> Feature service
  -> Feature repository
  -> ApplicationDbContext / SQL Server

Cross-cutting services: ASP.NET Identity, Stripe, SMTP, Cloudinary, hosted billing worker
```

## Runtime composition

`Program.cs` is the composition root. It registers MVC, localization, infrastructure, feature services, authentication and authorization, then maps the conventional MVC route and Identity Razor Pages.

`Extensions/ServiceCollectionExtensions.cs` centralizes dependency registration:

- `AddInfrastructure` configures SQL Server EF Core, ASP.NET Identity, claims/sign-in customizations, email, Cloudinary, Stripe, and the monthly billing hosted service.
- `AddFeatureServices` maps feature interfaces to scoped repositories and services.

The application uses the `en-US` culture with the currency symbol displayed as `tk`.

## Project layout

```text
ApartmentManagementSystem/
├── Domain/
│   ├── Constants/              Role names and composed role policies
│   └── Entities/               EF Core entities and domain data
├── Features/
│   └── <Feature>/
│       ├── <Feature>Controller.cs
│       ├── Services/           Workflow and authorization-aware business logic
│       ├── Repositories/       EF Core queries and persistence
│       └── ViewModels/         Request/view-specific models
├── Infrastructure/
│   ├── BackgroundJobs/         Hosted monthly tenant-bill generator
│   ├── Data/                   DbContext, Identity claims, and seeding
│   └── Services/               Email, uploads, code generation, custom sign-in
├── Views/                      Razor views grouped by controller
├── Areas/Identity/             Identity UI pages
├── wwwroot/                    Static CSS, JavaScript, animation, and vendor assets
├── Extensions/                 Service-registration extension methods
└── Program.cs                  Application startup and middleware pipeline
```

## Feature boundaries

The feature folders contain the current controllers, services, repositories, and view models for these areas:

| Area | Responsibility |
| --- | --- |
| Administration | User approval, role management, president assignment, and dashboards. |
| Buildings and Flats | Building/flat lifecycle and owner assignment. |
| Tenancy and Tenant Billing | Tenant directory/assignment, billing profiles, rent, and receipts. |
| Tenant Portal | Tenant-facing bills, payments, notices, visitors, and tickets. |
| Expenses and Owner Billing | Common bills, allocations, expense payments, owner balances, and receipts. |
| Payments | Stripe Checkout session creation, webhook handling, and payment persistence. |
| Announcements, Entry Logs, Maintenance | Day-to-day building operations. |
| President and Reports | Building dashboards and financial, occupancy, visitor, and maintenance reports. |

Controllers should remain focused on HTTP concerns: request binding, model validation, calling a service, and selecting a response or view. Services own workflows; repositories contain database access. New work should follow this separation rather than adding direct `ApplicationDbContext` calls to controllers.

## Identity and authorization

`ApplicationUser` extends ASP.NET Identity's user model with a full name, optional building, approval state, and profile image URL. The roles seeded at startup are `SuperAdmin`, `President`, `Owner`, `Tenant`, `Staff`, and `User`.

The custom claims principal factory exposes building information in the signed-in user's claims. Controllers use role attributes and services/repositories apply building and ownership boundaries where required. Authorization must be enforced server-side; sidebar visibility is only navigation assistance.

## Data model and invariants

`ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser>` and uses the default SQL schema `ams`.

```text
Building 1 ── * Flat
Building 1 ── * CommonBill 1 ── * ExpenseAllocation ── * ExpenseAllocationPayment
ApplicationUser (owner) 1 ── * Flat
Flat 1 ── 1 FlatBillingProfile
Flat 1 ── * TenantAssignment * ── 1 ApplicationUser (tenant)
TenantAssignment + FlatBillingProfile -> TenantBill 1 ── * TenantPayment
Building 1 ── * Announcement / EntryLog / MaintenanceTicket
```

Important database constraints include:

- Building names and building codes are unique.
- A flat number is unique within its building.
- A user can have at most one active tenant assignment, and a flat can have at most one active tenant assignment. Both use filtered unique indexes where `EndDate IS NULL`.
- A flat has at most one billing profile.
- Stripe/manual payment idempotency keys are unique when provided.
- Tenant bills use a row-version concurrency token.

## External integrations

### Payments

`StripePaymentService` creates Checkout sessions for tenant bills and owner common-bill allocations. Checkout metadata identifies the payment subject. The anonymous `POST /payments/webhook` endpoint verifies Stripe signatures and processes `checkout.session.completed` and `payment_intent.succeeded` events. Repository-level idempotency prevents duplicate payment records when both events arrive.

### Email and uploads

`PaymentEmailService` sends owner and tenant receipt emails through the registered SMTP sender. `CloudinaryPhotoUploadService` handles photo uploads when its configuration is supplied. Integrations should be treated as optional only where the relevant workflow handles their failure; never place keys in source-controlled configuration.

## Background work

`TenantMonthlyBillGenerator` is an in-process `BackgroundService`. Shortly after midnight each day, it checks whether it is the first day of the month. If so, it creates one bill for every active tenant assignment with an active flat billing profile, skipping bills that already exist for the same flat, tenant, and month.

Because this worker runs in the web process, deploy a single application instance or add distributed coordination before scaling out. It uses server-local `DateTime` values, so ensure the host timezone matches the intended billing calendar.

## Startup and deployment notes

On startup the application attempts to seed roles and the configured super-admin account. It requires a valid SQL connection and a `SuperAdminPassword`; it logs seeding failures rather than terminating the process. Stripe client configuration is also required when payment services are resolved.

The repository currently contains no EF Core migrations. A deployment must supply a compatible schema or create and manage migrations before running the application. See the [README](README.md#quick-start) for a new-local-database workflow.
