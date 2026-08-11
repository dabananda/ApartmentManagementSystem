# Feature-based conversion map

The application is an ASP.NET Core MVC (.NET 8) monolith using EF Core SQL Server,
ASP.NET Core Identity, Stripe and Cloudinary. MVC routes, controller names, views,
request binding and authorization attributes remain its public contract.

## Implemented feature boundaries

| Feature | Controller | Service | Repository |
| --- | --- | --- | --- |
| Announcements | `AnnouncementController` | `IAnnouncementService` | `IAnnouncementRepository` |
| Maintenance | `MaintenanceController` | `IMaintenanceService` | `IMaintenanceTicketRepository` |
| Expenses | `ExpenseAllocationController` | `IExpenseAllocationService` | `IExpenseAllocationRepository` |
| Common bills | `CommonBillController` | `ICommonBillService` | `ICommonBillRepository` |
| Expense payments | `ExpensePaymentController` | `IExpensePaymentService` | `IExpensePaymentRepository` |
| Tenant directory | `TenantController` | `ITenantDirectoryService` | `ITenantDirectoryRepository` |
| Flat billing profiles | `FlatBillingProfileController` | `IFlatBillingProfileService` | `IFlatBillingProfileRepository` |
| Entry logs | `EntryLogController` | `IEntryLogService` | `IEntryLogRepository` |
| Flats | `FlatController` | `IFlatService` | `IFlatRepository` |
| Buildings | `BuildingController` | `IBuildingService` | `IBuildingRepository` |
| Super Admin dashboard | `SuperAdminController` | `ISuperAdminDashboardService` | `ISuperAdminDashboardRepository` |
| President dashboard | `PresidentController` | `IPresidentDashboardService` | `IPresidentDashboardRepository` |

These feature implementations reside beneath `ApartmentManagementSystem/Features`.
- President Dashboard & Reports (`Features/President/`) -> **Migrated**
- Admin Panel (`Features/Administration/`) -> **Migrated**
- Owner Billing/Payments (`Features/Owner/`) -> **Migrated**
- Tenant Rent/Payments (`Features/TenantBilling/`) -> **Migrated**
- Tenant Portal (`Features/TenantPortal/`) -> **Migrated**
- Stripe Payments/Webhooks (`Features/Payments/`) -> **Migrated**

Controllers now retain only HTTP concerns: current-user resolution, model-state
handling, result selection and presentation state. Services own workflows and
repositories own EF Core queries and persistence.

## Remaining feature map

| Feature | Current MVC entry points | Primary data / integration dependencies |
| --- | --- | --- |
| Administration | `AdminController` | Identity, buildings, roles |
| Owner billing | `OwnerController`, `OwnerBillingController` | allocations, payments, email |
| Tenant assignment and billing | `TenantAssignmentController`, `TenantRentController`, `TenantPortalController` | assignments, bills, payments, email |
| Payments | `PaymentsController` | Stripe, tenant and owner payments |
| Reporting | `PresidentReportsController` | aggregated EF Core queries |

The remaining controllers contain direct EF Core access and should follow the same
incremental extraction pattern. They were deliberately left unchanged in this pass:
their workflows have more public branches and changing them without focused
endpoint tests would violate the behavior-preservation requirement.

## Dependency direction

`Controller -> feature service -> feature repository -> ApplicationDbContext`

Cross-cutting infrastructure remains in `Data` and `Services` until it can be
extracted without changing hosted-service, Identity, Cloudinary, Stripe or email
behavior. `TenantMonthlyBillGenerator` is a hosted infrastructure workflow and is
not an MVC controller concern.
