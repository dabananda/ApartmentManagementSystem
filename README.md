<div align="center">

# Apartment Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](http://makeapullrequest.com)
[![Architecture: Clean](https://img.shields.io/badge/Architecture-Clean-orange?style=flat-square)]()
[![Pattern: CQRS](https://img.shields.io/badge/Pattern-CQRS-blue?style=flat-square)]()

Apartment Management System is an enterprise-grade, role-based web application designed for operating and managing apartment buildings efficiently.

</div>

<hr />

## 📖 Overview

Built with **.NET 8**, **Entity Framework Core**, and **SQL Server**, this application is engineered using **Clean Architecture**, **Domain-Driven Design (DDD)**, and the **CQRS (Command Query Responsibility Segregation)** pattern.

It supports comprehensive workflows for building and flat administration, user approval, ownership and tenancy, common-expense allocation, tenant rent billing, payments (including Stripe integration), visitor logs, notices, maintenance tickets, and operational reports.

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

*Authorization is robustly enforced via claims. A president needs a building assignment before building-specific workflows are available.*

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

# Configure development-only secrets; replace the sample values.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=ApartmentManagementSystem;Trusted_Connection=True;TrustServerCertificate=True" --project AMS.Web
dotnet user-secrets set "SuperAdminEmail" "admin@example.test" --project AMS.Web
dotnet user-secrets set "SuperAdminPassword" "Use-a-strong-local-password" --project AMS.Web
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project AMS.Web
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project AMS.Web
dotnet user-secrets set "Stripe:Currency" "bdt" --project AMS.Web

# Initialize Database (Targeting the Infrastructure project where DbContext lives)
dotnet ef migrations add InitialCreate --project AMS.Infrastructure --startup-project AMS.Web
dotnet ef database update --project AMS.Infrastructure --startup-project AMS.Web

# Run the app
dotnet run --project AMS.Web
```

The HTTPS launch profile uses `https://localhost:7033`; HTTP uses `http://localhost:5117`.

At startup, the application creates its roles and the configured `SuperAdmin`. Sign in and begin by creating a building and assigning its president.

> [!WARNING]
> Do not use placeholder credentials outside a disposable local environment. Use User Secrets or environment variables for sensitive settings.

## 🛠 Configuration

| Key | Required | Purpose |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | **Yes** | SQL Server connection string. |
| `SuperAdminEmail` | **Yes** | The email address of the seeded root user. |
| `SuperAdminPassword` | **Yes** | The password of the seeded root user. |
| `Stripe:SecretKey` | No | Enables Stripe payment processing. |
| `Stripe:WebhookSecret` | No | Required for the application to accept Stripe webhook events and automatically reconcile payments. |
| `Stripe:Currency` | No | e.g. `usd`, `eur`, `bdt`. |
| `Cloudinary:CloudName` | No | Cloudinary environment details. Enables image uploads for profile pictures and maintenance tickets. |
| `Cloudinary:ApiKey` | No | |
| `Cloudinary:ApiSecret` | No | |
| `Email:SmtpServer` | No | SMTP settings. Enables the application to send payment receipts via email. |
| `Email:SmtpPort` | No | |
| `Email:SmtpUsername` | No | |
| `Email:SmtpPassword` | No | |
| `Email:FromAddress` | No | |
| `Email:FromName` | No | |

## 📚 Documentation

- [**Architecture Guide**](ARCHITECTURE.md): Deep dive into the Clean Architecture, CQRS implementation, Custom Mediator, and Domain Driven Design layers.
- [**Usage Guide**](USAGE.md): Walkthrough of standard operating procedures from SuperAdmin setup down to Tenant usage.
- [**Contributing Guidelines**](CONTRIBUTING.md): Instructions for developers.

## 💻 Technology Stack

**Core Architecture & Patterns:**
- Clean Architecture (Domain, Application, Infrastructure, Web layers)
- Domain-Driven Design (DDD)
- CQRS (Command Query Responsibility Segregation)
- Custom Mediator Pattern

**Backend:**
- .NET 8 (C# 12)
- ASP.NET Core MVC & Identity
- Entity Framework Core 8
- SQL Server

**Frontend:**
- Razor Pages / Views
- Bootstrap 5
- jQuery & DataTables
- FontAwesome

**Integrations:**
- Stripe (Payments & Webhooks)
- Cloudinary (Image Hosting)
- MailKit (SMTP Email)

## 🤝 Contributing

Contributions are welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📝 License & Usage

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details. 

## 👨‍💻 Author

**Dabananda Mitra**  
[GitHub](https://github.com/dabananda) | [LinkedIn](https://www.linkedin.com/in/dabanandamitra/) | [Portfolio](https://dabanandamitra.com)
