\# Apartment Management System



A role‑based web app for running an apartment building: buildings \& flats, owner/tenant management, common bills \& expense allocations, rent/billing, entry/visitor logs, maintenance tickets, and a tenant self‑service portal.



---



\## ✨ Features



\* \*\*Authentication \& Roles\*\*: Identity‑based auth with roles: `SuperAdmin`, `President`, `Owner`, `Staff`, `Tenant` (plus a pending `User` role). SuperAdmin is seeded automatically.

\* \*\*Buildings \& Flats\*\*



&nbsp; \* Unique \*\*building code\*\* auto‑generated like `BID1001`, `BID1002`, …

&nbsp; \* CRUD for buildings; per‑building stats (flats, owners, tenants, president).

\* \*\*Owner \& Tenant Management\*\*



&nbsp; \* Assign tenants to flats, manage owned flats, create tenant accounts.

\* \*\*Common Bills \& Expense Allocations\*\*



&nbsp; \* Record building‑level bills and allocate amounts to owners; track payments and remaining dues.

\* \*\*Tenant Billing (Owner → Tenant)\*\*



&nbsp; \* Per‑flat \*\*billing profile\*\* (title + monthly amount).

&nbsp; \* \*\*Monthly bill generation\*\* on the 1st (background worker).

&nbsp; \* Payments tracked against tenant bills with running \*\*Paid/Due\*\* totals.

\* \*\*Tenant Portal\*\*



&nbsp; \* Dashboard cards (totals, paid this month), recent bills \& payments, and building notices.

&nbsp; \* Pages for \*\*Bills\*\*, \*\*Payments\*\*, \*\*Notices\*\*, \*\*Tickets\*\* (create \& list).

\* \*\*Entry / Visitor Logs\*\* per flat/building.

\* \*\*Email (SMTP)\*\* using `IEmailSender` (password SMTP; SSL enabled).



---



\## 🧱 Tech Stack



\* \*\*.NET\*\* 8 (ASP.NET Core MVC + Identity)

\* \*\*EF Core\*\* with SQL Server

\* \*\*Razor Views\*\* (Bootstrap Icons, minor layout customizations)

\* \*\*BackgroundService\*\* for monthly billing



---



\## 📁 Project Structure



```

ApartmentManagementSystem/

&nbsp; Controllers/          # MVC controllers (Buildings, Owner, Tenant, Portal, …)

&nbsp; Data/                 # ApplicationDbContext, DbInitializer

&nbsp; Models/               # Entity models (Building, Flat, Tenant, Bills, …)

&nbsp; Services/             # EmailSender, BuildingCodeGenerator, background jobs

&nbsp; ViewModels/           # View models for pages \& tables

&nbsp; Views/                # Razor views

&nbsp; wwwroot/              # Static assets

&nbsp; Program.cs            # Host \& middleware setup

&nbsp; appsettings\*.json     # Configuration (see below)

```



---



\## 🚀 Getting Started



\### Prerequisites



\* \*\*.NET 8 SDK\*\*

\* \*\*SQL Server\*\* (LocalDB or full SQL Server)



\### 1) Clone \& Restore



```bash

\# clone

git clone https://github.com/dabananda/ApartmentManagementSystem.git

cd ApartmentManagementSystem/ApartmentManagementSystem



\# restore

dotnet restore

```



\### 2) Configure appsettings / secrets



Choose one of the following:



\*\*Option A — user secrets (recommended for dev):\*\*



```bash

\# inside the ApartmentManagementSystem/ApartmentManagementSystem directory

dotnet user-secrets init



\# required

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AMS;Trusted\_Connection=True;TrustServerCertificate=True;"



\# seeded super admin password (pick a strong one)

dotnet user-secrets set "SuperAdminPassword" "ChangeThis!123"



\# SMTP (example values)

dotnet user-secrets set "Smtp:Host" "smtp.example.com"

dotnet user-secrets set "Smtp:Port" "587"

dotnet user-secrets set "Smtp:From" "noreply@example.com"

dotnet user-secrets set "Smtp:User" "smtp-user"

dotnet user-secrets set "Smtp:Password" "smtp-password"

```



\*\*Option B — appsettings.Development.json:\*\*



```jsonc

{

&nbsp; "ConnectionStrings": {

&nbsp;   "DefaultConnection": "Server=.;Database=AMS;Trusted\_Connection=True;TrustServerCertificate=True;"

&nbsp; },

&nbsp; "SuperAdminPassword": "ChangeThis!123",

&nbsp; "Smtp": {

&nbsp;   "Host": "smtp.example.com",

&nbsp;   "Port": 587,

&nbsp;   "From": "noreply@example.com",

&nbsp;   "User": "smtp-user",

&nbsp;   "Password": "smtp-password"

&nbsp; }

}

```



\### 3) Database setup



```bash

\# create / update the database

dotnet ef database update

```



> The app runs EF Core migrations automatically if included; otherwise use the command above.



\### 4) Run



```bash

dotnet run

```



Open \[http://localhost:5000](http://localhost:5000) (or the shown URL).



---



\## 🔐 Seeded Users \& Roles



\* On first run, the app seeds roles: \*\*SuperAdmin, President, Owner, Tenant, Staff, User\*\*.

\* It also seeds a \*\*SuperAdmin\*\* account:



&nbsp; \* \*\*Email:\*\* `superadmin@ams.com`

&nbsp; \* \*\*Password:\*\* pulled from configuration key `SuperAdminPassword`.



Log in with the SuperAdmin to invite/approve others, create buildings, assign roles, etc.



---



\## 🧮 Billing System Overview



\### Owner → Tenant monthly bills



\* Define a \*\*Flat Billing Profile\*\* per flat with a title (e.g., "Monthly Rent") and amount.

\* On the \*\*1st day\*\* of each month, the background job creates a \*\*TenantBill\*\* for every active tenant assignment that has an active profile.

\* Payments (`TenantPayment`) reduce the Due amount; totals appear in the Tenant Portal.



\### Common building bills (shared expenses)



\* Create a `CommonBill` at the building level.

\* Add `ExpenseAllocation` entries per owner (Amount Due).

\* Record owner payments (`ExpenseAllocationPayment`); remaining due is computed.



---



\## ✉️ Email (SMTP)



\* The built‑in `EmailSender` uses the `Smtp` section from configuration.

\* SSL is enabled; credentials are required (no default credentials).



---



\## 🧩 Notable Implementation Details



\* \*\*Unique Building Codes\*\*: Generated via `BuildingCodeGenerator` as `BID####` and enforced on create.

\* \*\*Identity Integration\*\*: `ApplicationUser` extends `IdentityUser` with `Fullname`, `BuildingId`, approval flags, and ownership relations.

\* \*\*Tenant Portal\*\*: Controller uses view models to show recent bills/payments/notices; Bills and Payments pages list full histories.

\* \*\*Background Service\*\*: `TenantMonthlyBillGenerator` runs nightly and triggers bill generation on day 1.

\* \*\*Entry Logs\*\*: `EntryLog` model captures visitor type, purpose, people count, timestamps, and links to Building/Flat.



---



\## 🧪 Useful Commands



```bash

\# run the app

dotnet run



\# add a new migration

dotnet ef migrations add <Name>



\# update database

dotnet ef database update

```



---



\## 📄 License



Add a license of your choice (MIT recommended) at the repo root as `LICENSE`.



---



\## 🙌 Contributing



PRs are welcome! Please open an issue to discuss major changes.



