# 📖 Usage Guide

This guide describes the standard operating sequence for the web application. Because the system is built with **Clean Architecture and CQRS**, all actions below translate into strict, testable Commands and Queries under the hood.

---

## 🛠️ Initial Setup

1. **Sign In**: Log in as the seeded `SuperAdmin` configured through your `appsettings.json` or user secrets (`SuperAdminEmail` and `SuperAdminPassword`).
2. **Create a Building**: Navigate to the **Buildings** section, create a new building, and automatically/manually add its flats.
3. **Assign a President**: Go to **Assign President** to associate a President role with the building. This grants them operational control over the specific building.
4. **Populate Users**: The President can now begin creating and approving owners and tenants for that building.
5. **Assign Flats**: Assign owners to their respective flats, then assign each tenant to a flat to build the residency map.
6. **Billing Setup**: Configure an active billing profile for every flat that should receive monthly automated rent bills.

> [!CAUTION]
> Only **one active tenant assignment** is allowed for a tenant and for a flat at any given time. You must formally end an existing assignment before re-assigning a tenant to a new flat.

---

## 🛡️ SuperAdmin Operations

The SuperAdmin dashboard provides global administration and cross-building visibility.

- **Buildings:** Create, edit, inspect, and delete buildings.
- **Flats:** Inspect all flats globally and assign owners.
- **Assign President:** Link a president to a building.
- **Pending Users:** Review accounts awaiting approval across the system.
- **Users:** Edit user details, approve, reset, block/unblock, delete, or change roles.
- **Create User:** Manually provision accounts bypassing self-registration.

---

## 🏢 President Operations

After the SuperAdmin links a President to a Building, the President gains localized operational control over that domain.

- **All Users / Pending Users:** Create, approve, and manage users belonging exclusively to their assigned building.
- **Flats / Tenants:** View property inventory and active tenant information.
- **Common Bills:** Create building-wide expenses (e.g., utility bills, maintenance fees) and distribute their allocations to flat owners.
- **Owner Bills:** View aggregated owner balances for common expenses.
- **Bill Pay:** Record and reconcile expense payments.
- **Maintenance:** Review, prioritize, and resolve maintenance tickets raised by tenants.
- **Reports:** Generate and export financial, occupancy, visitor, and maintenance reports in CSV format.

> [!NOTE]
> Presidents can simultaneously possess the `Owner` role, granting them both operational tools and personal ownership dashboards in the navigation menu.

---

## 🔑 Owner Operations

Owners are assigned to specific flats and manage the tenancy lifecycle for those properties.

- **My Flats:** View all owned properties across any building.
- **Tenants:** Create new tenant accounts and assign them to owned flats.
- **Billing Profiles:** Set the monthly rent and expected service fees for each flat.
- **Rent Invoices:** Generate manual bills or review automatically generated monthly rent invoices for their tenants.
- **Collect Rent:** Log payments made by tenants and automatically issue email receipts.
- **My Expenses:** View and pay allocations for common building bills issued by the President.

---

## 🏠 Tenant Operations

Tenants have a restricted view, focusing solely on their personal occupancy.

- **Dashboard:** View current balance, recent bills, and active announcements.
- **My Rent:** View itemized rent bills and historical payment receipts.
- **Pay Online:** (If Stripe is configured) Pay outstanding rent securely via Stripe Checkout.
- **Maintenance:** Submit new maintenance requests (including photo uploads) and track resolution progress.
- **Visitors:** Pre-register expected guests or view historical entry logs for their flat.

---

## 👮 Staff Operations

Staff members (e.g., security guards or front desk clerks) manage the physical perimeter.

- **Entry Logs:** Record the arrival and departure of visitors. Select the destination flat to ensure accurate reporting and tenant visibility.
