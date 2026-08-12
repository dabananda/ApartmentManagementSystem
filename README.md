# Apartment Management System

Apartment Management System is a role-based web application for operating apartment buildings. It supports building and flat administration, user approval, ownership and tenancy, common-expense allocation, tenant rent billing, payments, visitor logs, notices, maintenance tickets, and operational reports.

The application is an ASP.NET Core MVC application targeting .NET 8 and uses SQL Server through Entity Framework Core.

## Contents

- [Capabilities](#capabilities)
- [Roles](#roles)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Documentation](#documentation)
- [Technology](#technology)

## Capabilities

- Create buildings and flats, assign owners, and maintain per-building records.
- Create, approve, block, reset, edit, and role-manage user accounts.
- Assign a president to a building and scope building operations to that user.
- Create tenant accounts, assign tenants to flats, and configure monthly flat billing profiles.
- Generate, allocate, and collect common bills from owners.
- Record manual tenant and owner payments, issue receipts, and send receipt emails when SMTP is configured.
- Accept Stripe Checkout payments and reconcile them from Stripe webhooks.
- Give tenants a portal for bills, payments, announcements, visitors, and maintenance tickets.
- Record entry/visitor logs and manage maintenance tickets.
- Produce financial, occupancy, visitor, and maintenance reports, including CSV exports.

## Roles

| Role | Primary responsibilities |
| --- | --- |
| `SuperAdmin` | Global administration, buildings, presidents, and users. |
| `President` | Building operations, approvals, flats, expenses, reports, and maintenance. |
| `Owner` | Owned flats, tenant creation/assignment, billing profiles, tenant-rent collection, and common-bill payments. |
| `Tenant` | Personal billing, payments, notices, visitors, and maintenance tickets. |
| `Staff` | Entry-log operations. |
| `User` | A seeded role available for pending/general accounts. |

Authorization is enforced in controllers. A president needs a building assignment before building-specific navigation and workflows are available.

## Quick start

### Prerequisites

- .NET 8 SDK
- SQL Server, SQL Server Express, or LocalDB
- EF Core CLI (`dotnet tool install --global dotnet-ef`) when creating/updating a database schema
- Optional: Stripe CLI for local payment-webhook testing

### Clone, configure, and run

```powershell
git clone https://github.com/dabananda/ApartmentManagementSystem.git
cd ApartmentManagementSystem
dotnet restore

# Configure development-only secrets; replace the sample values.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=ApartmentManagementSystem;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "SuperAdminEmail" "admin@example.test"
dotnet user-secrets set "SuperAdminPassword" "Use-a-strong-local-password"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
dotnet user-secrets set "Stripe:Currency" "bdt"

# This repository currently has no checked-in EF Core migrations.
# Create an initial migration for a new local database, then apply it.
dotnet ef migrations add InitialCreate --project ApartmentManagementSystem
dotnet ef database update --project ApartmentManagementSystem

dotnet run --project ApartmentManagementSystem
```

The HTTPS launch profile uses `https://localhost:7033`; the HTTP profile uses `http://localhost:5117`. See `ApartmentManagementSystem/Properties/launchSettings.json` for the authoritative local URLs.

At startup, the application creates its roles and the configured `SuperAdmin` account if they do not already exist. Sign in with the configured super-admin email and password, then create a building and assign its president.

Do not use the placeholder credentials in `appsettings.Development.json` outside a disposable local environment. Prefer User Secrets or environment variables for all sensitive settings.

## Configuration

The settings below are read from configuration. User Secrets are appropriate for local development; production should provide them through its secret-management/environment configuration.

| Key | Required | Purpose |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Yes | SQL Server connection string. |
| `SuperAdminEmail` | Yes | Email for the initial super-admin seed account. |
| `SuperAdminPassword` | Yes | Password for the initial super-admin seed account. |
| `Stripe:SecretKey` | Yes at runtime | Stripe API client key. |
| `Stripe:WebhookSecret` | Yes for webhooks | Verifies incoming Stripe webhooks. |
| `Stripe:Currency` | No | Checkout currency; defaults to `bdt`. |
| `Smtp:*` | No | SMTP host, port, sender, username, and password for receipt email. |
| `Cloudinary:*` | No | Cloudinary cloud name, API key, and API secret for photo uploads. |

For local Stripe testing, run the application and then forward Stripe events to its webhook endpoint:

```powershell
stripe listen --forward-to https://localhost:7033/payments/webhook
```

Store the signing secret printed by the Stripe CLI as `Stripe:WebhookSecret`.

## Documentation

- [Usage guide](USAGE.md) — role-based operational workflows.
- [Architecture guide](ARCHITECTURE.md) — system boundaries, data model, dependencies, and runtime behavior.
- [Contribution guide](CONTRIBUTING.md) — local development and change-submission conventions.

## Technology

- ASP.NET Core MVC and Razor Views (.NET 8)
- ASP.NET Core Identity for accounts and roles
- Entity Framework Core 8 with SQL Server
- Stripe Checkout and webhooks for online payments
- SMTP for receipt delivery and Cloudinary for image uploads
- Bootstrap, JavaScript, and CSS assets served from `wwwroot`

## License

This project is released under the [MIT License](LICENSE).
