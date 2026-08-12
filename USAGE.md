# 📖 Usage Guide

This guide describes the normal operating sequence for the web application. Sign in through the Identity account pages, then use the role-specific dashboard and sidebar.

---

## 🛠️ Initial Setup

1. **Sign In**: Log in as the seeded `SuperAdmin` configured through `SuperAdminEmail` and `SuperAdminPassword`.
2. **Create a Building**: Open **Buildings**, create a new building, and add its flats.
3. **Assign a President**: Navigate to **Assign President** and associate a president with the building.
4. **Populate Users**: The president can now create and approve the owners and tenants for that building.
5. **Assign Flats**: Assign owners to their respective flats, then assign each tenant to a flat.
6. **Billing Setup**: Configure an active billing profile for every flat that should receive monthly rent bills.

> [!CAUTION]
> Only **one active tenant assignment** is allowed for a tenant and for a flat at any given time. You must end an existing assignment before re-assigning that tenant or flat.

---

## 🛡️ SuperAdmin Operations

The SuperAdmin dashboard provides global administration.

- **Buildings:** Create, edit, inspect, and delete buildings.
- **Flats:** Inspect all flats and assign owners.
- **Assign President:** Link a president to a building.
- **Pending Users:** Review accounts awaiting approval.
- **Users:** Edit user details, approve, reset, block/unblock, delete, or change roles.
- **Create User:** Manually create accounts bypassing registration.

---

## 🏢 President Operations

After the SuperAdmin links a president to a building, the president gains operational control of that building.

- **All Users / Pending Users:** Create, approve, and manage building users.
- **Flats / Tenants:** View property inventory and tenant information.
- **Common Bills:** Create building-wide expenses and inspect their allocations.
- **Owner Bills:** View owner balances for common expenses.
- **Bill Pay:** Record expense payments.
- **Maintenance:** Review and manage maintenance tickets.
- **Reports:** Open financial, occupancy, visitor, and maintenance reports. CSV export actions are available from these pages.

*Note: Presidents can also have the `Owner` role, in which case both sets of navigation options appear.*

---

## 🔑 Owner Operations

Owners manage their assigned flats.

1. **Create Tenant:** Make a tenant account for the building.
2. **Assign Tenant:** Give that tenant an active flat assignment.
3. **Billing Profiles:** Set the title, monthly amount, and active state for each billable flat.
4. **Tenant Rent:** See a tenant's bills, record a payment, collect a full bill, collect all due bills, and view/email receipts.
5. **Common Bills:** Review shared-expense allocations and make an online payment when Stripe is configured.

The owner dashboard and **My Flats** page summarize the flats the current owner owns.

---

## 👤 Tenant Operations

Tenants use **My Dashboard** and the **Tenant Portal**.

- **My Bills:** Inspect current and past bills. Start an online checkout for an unpaid amount.
- **My Payments:** Review recorded payments.
- **Notices:** Read announcements published for the building.
- **Maintenance:** Create tickets and monitor existing tickets.
- **Visitors:** View relevant visitor/entry records for the selected date range.

> [!NOTE]
> If the portal shows a setup-required message, ask the building president or owner to create an active tenant-to-flat assignment.

---

## 🛂 Staff Operations

Staff members can use **Entry Logs** to record, inspect, edit, and remove visitor/entry records within their authorized building context. Owners, presidents, and SuperAdmins also have entry-log access.

---

## 💳 Billing and Payment Behavior

### Common Bills
A president or SuperAdmin creates a common bill for a building. The application records owner allocations against that bill. Owners can pay an outstanding allocation through Stripe Checkout. Privileged users can manually review owner billing and record payments.

### Tenant Rent
An active flat billing profile supplies the title and monthly amount. The monthly bill generator runs automatically inside the web application after midnight on the first day of the month. It creates bills for active tenant assignments and intelligently skips already-existing bills for the same flat, tenant, and month.

An owner/president/SuperAdmin may record manual tenant payments. A tenant can use Stripe Checkout for a full or partial due amount. Receipt emails are sent automatically when SMTP is configured.

### Stripe Setup and Webhooks
Online payment completion depends on the Stripe webhook, not merely the return page. Configure `Stripe:SecretKey` and `Stripe:WebhookSecret`.

For local development, start a listener:
```powershell
stripe listen --forward-to https://localhost:7033/payments/webhook
```
The webhook safely handles duplicate events via strict payment idempotency safeguards.

---

## 🚑 Troubleshooting

| Symptom | What to check |
| :--- | :--- |
| **App does not seed an admin** | Verify the SQL connection, `SuperAdminEmail`, and `SuperAdminPassword`. Inspect application logs. |
| **President has no building controls** | Verify that their account has the `President` role and is explicitly linked to a building. |
| **Tenant cannot access bills** | Verify the tenant has an active assignment and the `Tenant` role. |
| **No monthly bill appears** | Verify it is after the worker's first-of-month run, the assignment is active, and the flat has an active billing profile. |
| **Stripe payment not recorded** | Verify the webhook secret, endpoint forwarding, Stripe test mode, and server logs. |
| **Receipt email fails** | Verify all `Smtp:*` configuration values and SMTP-provider authentication requirements. |
