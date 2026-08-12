<div align="center">

# Apartment Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](http://makeapullrequest.com)
[![Platform](https://img.shields.io/badge/Platform-Web-blue?style=flat-square)]()

Apartment Management System is a comprehensive role-based web application tailored for operating and managing apartment buildings efficiently.

</div>

<hr />

## 📖 Overview

It supports building and flat administration, user approval, ownership and tenancy, common-expense allocation, tenant rent billing, payments, visitor logs, notices, maintenance tickets, and operational reports.

Built with **ASP.NET Core MVC** targeting **.NET 8** and powered by **SQL Server** through **Entity Framework Core**.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Roles](#-roles)
- [Quick Start](#-quick-start)
- [Configuration](#-configuration)
- [Documentation](#-documentation)
- [Technology Stack](#-technology-stack)
- [Contributing](#-contributing)
- [License & Usage](#-license--usage)
- [Author](#-author)

---

## 🚀 Capabilities

- **Property Management**: Create buildings and flats, assign owners, and maintain per-building records.
- **User Management**: Create, approve, block, reset, edit, and role-manage user accounts. Assign a president to a building and scope operations.
- **Tenancy & Billing**: Create tenant accounts, assign tenants to flats, configure monthly flat billing profiles, generate and collect bills.
- **Payments**: Record manual tenant and owner payments, issue receipts, send receipt emails (SMTP), and accept **Stripe Checkout** payments seamlessly with automated reconciliation from Stripe webhooks.
- **Tenant Portal**: Empower tenants with a portal for bills, payments, announcements, visitors, and maintenance tickets.
- **Day-to-day Operations**: Record entry/visitor logs and manage maintenance tickets.
- **Reporting**: Produce comprehensive financial, occupancy, visitor, and maintenance reports, including CSV exports.

## 👥 Roles

| Role | Primary Responsibilities |
| :--- | :--- |
| `SuperAdmin` | Global administration, buildings, presidents, and users. |
| `President` | Building operations, approvals, flats, expenses, reports, and maintenance. |
| `Owner` | Owned flats, tenant creation/assignment, billing profiles, tenant-rent collection, and common-bill payments. |
| `Tenant` | Personal billing, payments, notices, visitors, and maintenance tickets. |
| `Staff` | Entry-log operations. |
| `User` | A seeded role available for pending/general accounts. |

*Authorization is robustly enforced in controllers. A president needs a building assignment before building-specific workflows are available.*

## ⚙️ Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server, SQL Server Express, or LocalDB
- EF Core CLI (`dotnet tool install --global dotnet-ef`)
- *Optional:* Stripe CLI for local payment-webhook testing

### Clone, Configure, and Run

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

# Initialize Database
dotnet ef migrations add InitialCreate --project ApartmentManagementSystem
dotnet ef database update --project ApartmentManagementSystem

# Run the app
dotnet run --project ApartmentManagementSystem
```

The HTTPS launch profile uses `https://localhost:7033`; HTTP uses `http://localhost:5117`.

At startup, the application creates its roles and the configured `SuperAdmin`. Sign in and begin by creating a building and assigning its president.

> [!WARNING]
> Do not use the placeholder credentials in `appsettings.Development.json` outside a disposable local environment. Use User Secrets or environment variables for sensitive settings.

## 🛠 Configuration

| Key | Required | Purpose |
| --- | :---: | --- |
| `ConnectionStrings:DefaultConnection` | **Yes** | SQL Server connection string. |
| `SuperAdminEmail` | **Yes** | Email for the initial super-admin seed account. |
| `SuperAdminPassword` | **Yes** | Password for the initial super-admin seed account. |
| `Stripe:SecretKey` | Yes (runtime) | Stripe API client key. |
| `Stripe:WebhookSecret` | Yes (webhooks)| Verifies incoming Stripe webhooks. |
| `Stripe:Currency` | No | Checkout currency; defaults to `bdt`. |
| `Smtp:*` | No | SMTP settings for receipt email. |
| `Cloudinary:*` | No | Cloudinary settings for photo uploads. |

For local Stripe testing:
```powershell
stripe listen --forward-to https://localhost:7033/payments/webhook
```

## 📚 Documentation

- [Usage Guide](USAGE.md) — Role-based operational workflows.
- [Architecture Guide](ARCHITECTURE.md) — System boundaries, data model, dependencies, and runtime behavior.
- [Contribution Guide](CONTRIBUTING.md) — Local development and change-submission conventions.
- [Code of Conduct](CODE_OF_CONDUCT.md) — Community standards and behavior.
- [Security Policy](SECURITY.md) — Reporting vulnerabilities.

## 💻 Technology Stack

- **Framework:** ASP.NET Core MVC and Razor Views (.NET 8)
- **Identity & Security:** ASP.NET Core Identity
- **ORM & Database:** Entity Framework Core 8 with SQL Server
- **Payments:** Stripe Checkout and Webhooks
- **Integrations:** SMTP, Cloudinary
- **Frontend Assets:** Bootstrap, JavaScript, CSS (`wwwroot`)

## 🤝 Contributing

We welcome contributions! Please see the [Contributing Guide](CONTRIBUTING.md) for details on how to get started, conventions to follow, and the submission process. Ensure you also review our [Code of Conduct](CODE_OF_CONDUCT.md).

## 📄 License & Usage

This project is open-source and released under the [MIT License](LICENSE). 

> **Commercial / Product Usage License:** 
> Interested persons or companies wishing to use this product in a commercial or extensive operational capacity must contact the author for a dedicated product usage license and support.

---

## 👨‍💻 Author

**Dabananda Mitra**  
*Software Engineer*

- **Portfolio:** [dabananda.vercel.app](https://dabananda.vercel.app)
- **LinkedIn:** [linkedin.com/in/dabananda](https://linkedin.com/in/dabananda)
- **GitHub:** [github.com/dabananda](https://github.com/dabananda)
- **X (Twitter):** [@dabanandamitra](https://x.com/dabanandamitra)
- **Facebook**: [fb.com/dabanandamitra](https://www.facebook.com/dabanandamitra/)
- **WhatsApp**: [wa.me/@dabananda](https://wa.me/@dabananda)


Feel free to connect or reach out for inquiries, licensing, or just to say hi!
