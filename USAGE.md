# Usage guide

This guide describes the normal operating sequence for the web application. Sign in through the Identity account pages, then use the role-specific dashboard and sidebar.

## Initial setup

1. Sign in as the seeded `SuperAdmin` configured through `SuperAdminEmail` and `SuperAdminPassword`.
2. Open **Buildings**, create the building, and add its flats.
3. Open **Assign President** and associate a president with the building.
4. The president can create/approve the owners and tenants for that building.
5. Assign owners to flats, then assign each tenant to a flat.
6. Configure an active billing profile for every flat that should receive monthly rent bills.

Only one active tenant assignment is allowed for a tenant and for a flat. End an existing assignment before assigning that tenant or flat again.

## SuperAdmin operations

The SuperAdmin dashboard provides global administration.

- Use **Buildings** to create, edit, inspect, and delete buildings.
- Use **Flats** to inspect all flats and assign owners.
- Use **Assign President** to link a president to a building.
- Use **Pending Users** to review accounts awaiting approval.
- Use **Users** to edit user details, approve, reset, block/unblock, delete, or change roles.
- Use **Create User** to create accounts directly.

## President operations

After the SuperAdmin links a president to a building, the president can operate that building.

- **All Users / Pending Users:** create, approve, and manage building users.
- **Flats / Tenants:** view property inventory and tenant information.
- **Common Bills:** create building-wide expenses and inspect their allocations.
- **Owner Bills:** view owner balances for common expenses.
- **Bill Pay:** record expense payments.
- **Maintenance:** review and manage maintenance tickets.
- **Reports:** open financial, occupancy, visitor, and maintenance reports; CSV export actions are available from report pages.

Presidents can also have the Owner role, in which case both sets of navigation options appear.

## Owner operations

Owners work with their assigned flats.

1. Use **Create Tenant** to make a tenant account for the building.
2. Use **Assign Tenant** to give that tenant an active flat assignment.
3. Use **Billing Profiles** to set the title, monthly amount, and active state for each billable flat.
4. Use **Tenant Rent** to see a tenant's bills, record a payment, collect a full bill, collect all due bills, and view/email receipts.
5. Use **Common Bills** to review shared-expense allocations and make an online payment when Stripe is configured.

The owner dashboard and **My Flats** page summarize the flats the current owner owns.

## Tenant operations

Tenants use **My Dashboard** and the Tenant Portal.

- **My Bills:** inspect current and past bills and start an online checkout for an unpaid amount.
- **My Payments:** review recorded payments.
- **Notices:** read announcements published for the building.
- **Maintenance:** create tickets and monitor existing tickets.
- **Visitors:** view relevant visitor/entry records for the selected date range.

If the portal shows a setup-required message, ask the building president or owner to create an active tenant-to-flat assignment.

## Staff operations

Staff members can use **Entry Logs** to record, inspect, edit, and remove visitor/entry records within their authorized building context. Owners, presidents, and SuperAdmins also have entry-log access.

## Billing and payment behavior

### Common bills

A president or SuperAdmin creates a common bill for a building. The application records owner allocations against that bill. Owners can pay an outstanding allocation through Stripe Checkout; privileged users can review owner billing and record payments where the available workflow permits it.

### Tenant rent

An active flat billing profile supplies the title and monthly amount. The monthly bill generator runs inside the web application after midnight on the first day of the month and creates bills for active tenant assignments. It skips an already-existing bill for the same flat, tenant, and month.

An owner/president/SuperAdmin may record manual tenant payments. A tenant can use Stripe Checkout for a full or partial due amount. Receipt emails are attempted when SMTP is configured.

### Stripe setup and webhooks

Online payment completion depends on the Stripe webhook, not merely the return page. Configure `Stripe:SecretKey` and `Stripe:WebhookSecret`; for local development, start a listener:

```powershell
stripe listen --forward-to https://localhost:7033/payments/webhook
```

The webhook accepts `checkout.session.completed` and `payment_intent.succeeded`. Duplicate event delivery is handled through payment idempotency safeguards.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| The application does not seed an admin | Verify the SQL connection, `SuperAdminEmail`, and `SuperAdminPassword`; inspect application logs for seeding errors. |
| A president sees no building-specific controls | Verify that their account has the `President` role and is linked to a building. |
| A tenant cannot access bills | Verify that the tenant has an active assignment and the correct role. |
| No monthly bill appears | Verify it is after the worker's first-of-month run, the assignment is active, and the flat has an active billing profile. |
| Stripe payment is not recorded | Verify the webhook secret, endpoint/CLI forwarding, Stripe test mode, and server logs. |
| Receipt email fails | Verify all `Smtp:*` configuration values and SMTP-provider authentication requirements. |
