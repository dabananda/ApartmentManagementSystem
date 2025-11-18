# Apartment Management System

A role-based ASP.NET Core MVC application to operate an apartment building: buildings & flats, owners & tenants, shared expenses, monthly rent billing, payments (manual & Stripe), entry/visitor logs, and maintenance tickets. Designed for multi-role workflows with per‑building data boundaries.

---

## Table of Contents

- [Features](#features)
- [Roles & Permissions](#roles--permissions)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Quick Start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [Clone & Restore](#1-clone--restore)
  - [Configure Secrets](#2-configure-secrets)
  - [Database](#3-database)
  - [Run the App](#4-run-the-app)
  - [Stripe: Local Webhook Setup](#stripe-local-webhook-setup)
- [Seeded Data](#seeded-data)
- [Payments](#payments)
  - [Manual Payments](#manual-payments)
  - [Stripe Checkout + Webhook](#stripe-checkout--webhook)
- [Tenant Assignment Rules](#tenant-assignment-rules)
- [Background Jobs](#background-jobs)
- [Troubleshooting](#troubleshooting)
- [Development Tips](#development-tips)
- [Contributing](#contributing)

---

## Features

- **Authentication & Roles (ASP.NET Identity)**: `SuperAdmin`, `President`, `Owner`, `Tenant`, `Staff`, `User` (pending).
- **Buildings & Flats**: CRUD with unique building codes (e.g., `BID1001`). Per-building stats.
- **Owners & Tenants**: Assign tenants to flats; owner-to-tenant billing profiles.
- **Common Bills & Allocations**: Create shared bills and allocate to owners; track payments & due.
- **Tenant Billing**:
  - Per‑flat billing profile (title + monthly amount).
  - **Monthly bill generation** on the 1st via background service.
  - Paid/Due totals visible in dashboards and portal.
- **Payments**:
  - **Manual** owner/tenant payments with email receipts.
  - **Stripe Checkout** for card payments + **webhook** reconciliation.
- **Tenant Portal**: Bills, payments, notices, tickets.
- **Entry/Visitor Logs** & **Maintenance Tickets**.
- **Email (SMTP)** with HTML receipts.

---

## Roles & Permissions

- **SuperAdmin**: Full control; can manage buildings and create any role.
- **President**: Building lead. May also hold the **Owner** role. Can create **Owner** and **Tenant** users for their building.
- **Owner**: Can create **Tenant** users for their building; manages flats and tenant rent.
- **Tenant**: Views bills, makes payments, creates tickets.
- **Staff/User**: Limited operational roles as configured.

> President may have both `President` and `Owner` roles. UI and server enforce: **President can create Owner & Tenant**; **Owner can create Tenant** only.

---

## Tech Stack

- **.NET 8** — ASP.NET Core MVC + Identity
- **EF Core** — SQL Server
- **Razor Views** — Bootstrap-based UI
- **BackgroundService** — monthly bill generator
- **Stripe** — Checkout & webhook for card payments

---

## Project Structure

```
ApartmentManagementSystem/
  Controllers/
  Data/
  Models/
  Services/
  ViewModels/
  Views/
  wwwroot/
  Program.cs
  appsettings*.json
```

---

## Quick Start

### Prerequisites

- **.NET 8 SDK**
- **SQL Server** (Developer/Express/LocalDB)
- **Stripe CLI** (for local webhooks): https://stripe.com/docs/stripe-cli

### 1) Clone & Restore

```bash
git clone https://github.com/dabananda/ApartmentManagementSystem.git
cd ApartmentManagementSystem/ApartmentManagementSystem
dotnet restore
```

### 2) Configure Secrets

Use **User Secrets** for local development.

```bash
# Initialize secrets store
dotnet user-secrets init

# Database connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AMS;Trusted_Connection=True;TrustServerCertificate=True;"

# Seeded SuperAdmin password
dotnet user-secrets set "SuperAdminPassword" "ChangeThis!123"

# SMTP (example values)
dotnet user-secrets set "Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:From" "noreply@example.com"
dotnet user-secrets set "Smtp:User" "smtp-user"
dotnet user-secrets set "Smtp:Password" "smtp-password"

# Stripe (dev) — keys & currency
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."   # comes from Stripe CLI output
dotnet user-secrets set "Stripe:Currency" "usd"
```

**Stripe config demo** (for reference only):

```json
"Stripe": {
  "PublishableKey": "",
  "SecretKey": "",
  "WebhookSecret": "",
  "Currency": ""
}
```

> You may also use `appsettings.Development.json` instead of user secrets if preferred.

### 3) Database

```bash
dotnet ef database update
```

### 4) Run the App

```bash
dotnet run
```
Open the printed HTTPS URL (e.g., `https://localhost:7033/`).

### Stripe: Local Webhook Setup

After the app is running, start the Stripe CLI listener **in a separate terminal**.  
Replace the port if your local HTTPS port differs (see `Properties/launchSettings.json`).

```powershell
stripe listen --forward-to https://localhost:7033/payments/webhook
```

The CLI prints a signing secret like:

```
Ready! Your webhook signing secret is: whsec_xxx
```

Copy that to user secrets as `Stripe:WebhookSecret` (see above). With the listener running, Stripe events reach your local app and **payments will be recorded**.

---

## Seeded Data

On first run the app seeds:

- Roles: **SuperAdmin, President, Owner, Tenant, Staff, User**
- **SuperAdmin** account:
  - **Email:** `superadmin@ams.com`
  - **Password:** value from `SuperAdminPassword`

Log in as SuperAdmin to create buildings, assign a President, and invite Owners/Tenants.

---

## Payments

### Manual Payments

- Owners and tenants can record manual payments (cash/bank).  
- After a successful save, the app attempts to send a **receipt email**. Email failures are logged and surfaced as a small warning, but **do not block** the request.

### Stripe Checkout + Webhook

- Checkout sessions are created server-side.
- The webhook endpoint `POST /payments/webhook` finalizes payments on `checkout.session.completed` or `payment_intent.succeeded` events.
- Idempotency is enforced to prevent duplicates.

**Local testing flow:**

1. Run the app.
2. Start the Stripe listener:
   ```powershell
   stripe listen --forward-to https://localhost:7033/payments/webhook
   ```
3. Use the UI to initiate a card payment, or trigger test events:
   ```bash
   stripe trigger checkout.session.completed
   stripe trigger payment_intent.succeeded
   ```

---

## Tenant Assignment Rules

- A tenant can have **only one active flat assignment** at a time.
- A flat can have **only one active tenant** at a time.
- Enforced by both UI and **database filtered unique indexes** (active = `EndDate IS NULL`).

This prevents double-assignments even under concurrent requests.

---

## Background Jobs

- `TenantMonthlyBillGenerator` runs daily and generates bills on the **1st** of each month based on **Flat Billing Profiles**.
- In production with multiple instances, run a single instance or add a distributed lock to avoid duplicate runs.
- Operates in UTC; display times are localized in the UI.

---

## Troubleshooting

- **Payments recorded but page showed an error**: likely SMTP connectivity. Email send is best‑effort; payment saves are not rolled back. Configure `Smtp:*` settings or disable email in dev.
- **Stripe webhook not firing**: ensure the app is running **and** the Stripe CLI listener is active, and the `Stripe:WebhookSecret` matches the latest CLI output.
- **DataTables warning about unknown parameter**: ensure the table columns in the view match the data or use named property bindings in DataTables configuration.

---

## Contributing

PRs are welcome! Please open an issue for discussion before large changes.
