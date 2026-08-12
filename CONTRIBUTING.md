# Contributing

Thank you for contributing to Apartment Management System. Keep changes focused, secure, and consistent with the feature-oriented design.

## Set up a development environment

1. Install the .NET 8 SDK and a local SQL Server instance.
2. Fork and clone the repository, then restore dependencies:

   ```powershell
   dotnet restore
   ```

3. Configure local secrets as described in the [README](README.md#configuration). Do not commit connection strings, Stripe keys, SMTP passwords, Cloudinary secrets, or production credentials.
4. Create/apply migrations for your local database if needed. This repository currently has no checked-in migrations:

   ```powershell
   dotnet ef migrations add InitialCreate --project ApartmentManagementSystem
   dotnet ef database update --project ApartmentManagementSystem
   ```

5. Run the application:

   ```powershell
   dotnet run --project ApartmentManagementSystem
   ```

## Development conventions

- Add application work under the appropriate `Features/<Feature>` folder.
- Keep controllers thin: bind and validate input, call a service, then return a result.
- Put workflow/business behavior in a service and EF Core queries/persistence in a repository.
- Prefer view models for UI input and output instead of binding entities broadly.
- Use asynchronous EF Core APIs and pass cancellation tokens where the existing convention supports them.
- Apply authorization at the controller/action level and enforce ownership/building boundaries in the workflow. Never rely on hidden navigation for access control.
- Preserve anti-forgery validation on state-changing browser endpoints.
- Use decimal amounts for money and maintain the existing idempotency/concurrency safeguards in payment flows.
- Treat Stripe webhooks as retryable and duplicate-delivery-prone.

## Database changes

Review changes to `ApplicationDbContext` carefully. Protect the existing invariants around unique building/flat identity, active tenancy, billing profiles, idempotent payments, and tenant-bill row versions.

For a shared project, include the generated migration files with schema changes and verify that a new database can be created from them. Never alter a migration that has already been used outside your local environment.

## Verify before submitting

Run the following from the repository root:

```powershell
dotnet restore
dotnet build ApartmentManagementSystem.sln
dotnet test ApartmentManagementSystem.sln
```

There are currently no test projects in the solution, so the last command may report that no tests were found. For changes without automated coverage, manually exercise the affected role and workflow. At minimum, verify authorization behavior, validation errors, and the success path.

For payment changes, use Stripe test keys and the Stripe CLI; never test against live credentials. For UI changes, check each relevant role because the sidebar and dashboards differ by role.

## Pull requests

- Use a focused branch and a clear, imperative commit message.
- Explain the problem, solution, schema/configuration changes, and verification steps in the pull request.
- Include screenshots for visible UI changes when useful.
- Call out changes affecting security, payments, billing generation, migrations, or external integrations.
- Keep unrelated formatting/refactoring out of the same pull request.

If a proposed change substantially alters workflows or data ownership, open an issue or discussion before implementation.
