# 🤝 Contributing to Apartment Management System

First off, thank you for considering contributing to the Apartment Management System! It's people like you that make open-source projects thrive. We welcome all contributions, including bug reports, feature requests, documentation improvements, and code changes.

Please take a moment to review this document to make the contribution process easy and effective for everyone involved. Also, please refer to our [Code of Conduct](CODE_OF_CONDUCT.md) before you start.

## 🛠️ Set up a Development Environment

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download) and a local SQL Server instance.
2. Fork and clone the repository, then restore dependencies:

   ```powershell
   dotnet restore
   ```

3. Configure local secrets as described in the [README](README.md#configuration) under the `AMS.Web` project. Do not commit connection strings, Stripe keys, SMTP passwords, Cloudinary secrets, or production credentials.
4. Create and apply migrations for your local database. Migrations reside in the `AMS.Infrastructure` project, but the startup project is `AMS.Web`:

   ```powershell
   dotnet ef migrations add InitialCreate --project AMS.Infrastructure --startup-project AMS.Web
   dotnet ef database update --project AMS.Infrastructure --startup-project AMS.Web
   ```

5. Run the application:

   ```powershell
   dotnet run --project AMS.Web
   ```

## 📐 Development Conventions (Clean Architecture & CQRS)

- **Domain First:** Add core business rules, Entities, and Value Objects to `AMS.Domain`. This layer has no dependencies.
- **CQRS:** Add new application features as Commands (writes) or Queries (reads) inside `AMS.Application/Features/<Feature Area>/`.
- **Handlers:** Put workflow/business behavior inside a specific `IRequestHandler` in the Application layer, executed via the Custom Mediator.
- **Infrastructure:** Put EF Core queries, persistence implementations, and external integrations in `AMS.Infrastructure`. 
- **Thin Controllers:** Keep controllers in `AMS.Web` extremely thin: parse the HTTP request, map it to a Command/Query, dispatch it to the Mediator, and return a View or HTTP Result.
- **Thin DbContext:** Entity configurations should be placed in `AMS.Infrastructure/Data/Configurations` implementing `IEntityTypeConfiguration<T>`. Do not bloat `ApplicationDbContext.OnModelCreating`.
- **Authorization:** Apply authorization at the controller/action level and enforce ownership/building boundaries in the Handlers. **Never rely on hidden navigation for access control.**

## 🗄️ Database Changes

Review changes to `ApplicationDbContext` carefully. Protect the existing invariants around unique building/flat identity, active tenancy, billing profiles, idempotent payments, and tenant-bill row versions.

For a shared project, include the generated migration files with schema changes and verify that a new database can be created from them. **Never alter a migration that has already been used outside your local environment.**

## ✅ Verify Before Submitting

Run the following from the repository root:

```powershell
dotnet clean
dotnet build
```

For changes without automated coverage, manually exercise the affected role and workflow. At minimum, verify authorization behavior, validation errors, and the success path.

For payment changes, use Stripe test keys and the Stripe CLI; never test against live credentials. For UI changes, check each relevant role because the sidebar and dashboards differ by role.

## 📝 Pull Requests

- Use a focused branch and a clear, imperative commit message.
- Explain the problem, solution, schema/configuration changes, and verification steps in the pull request.
- Include screenshots for visible UI changes when useful.
- Call out changes affecting security, payments, billing generation, migrations, or external integrations.
- Keep unrelated formatting/refactoring out of the same pull request.

> [!TIP]
> If a proposed change substantially alters workflows or data ownership, please open an issue or discussion before implementation so we can align on the design!

## 📬 Need Help?

If you have questions about where to start, feel free to reach out to the author:
> **Dabananda Mitra** - [LinkedIn](https://www.linkedin.com/in/dabananda/), [WhatsApp](https://wa.me/@dabananda), [GitHub](https://github.com/dabananda), [Email](mailto:dabananda.dev@gmail.com) or [Facebook](https://facebook.com/dabanandamitra)
