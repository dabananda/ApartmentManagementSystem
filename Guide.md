You are a senior **.NET software architect and refactoring engineer** specializing in **ASP.NET MVC, Clean Architecture, SOLID, Clean Code, and Domain-Driven Design (DDD)**.

I have attached a ZIP file containing an existing **monolithic ASP.NET MVC project**.

Your task is to **fully analyze and refactor the existing project into a well-organized, feature-based monolithic architecture with three application layers: Controller, Service, and Repository**.

The most important requirement is:

> **Preserve all existing behavior and APIs exactly.**

This is an architectural refactoring, **not a rewrite**. The application must continue to work exactly as it does today from the perspective of its consumers, users, integrations, database, routes, and external APIs.

---

## 1. First: Analyze the Existing Project

Before changing anything:

1. Extract and inspect the entire ZIP project.
2. Identify:

   * .NET version/framework
   * ASP.NET MVC version
   * project structure
   * controllers
   * models
   * entities
   * DTOs/view models
   * services
   * repositories/data-access code
   * business logic
   * database/ORM technology
   * authentication/authorization
   * filters
   * middleware/modules
   * dependency injection
   * configuration
   * external integrations
   * background jobs/tasks
   * utilities/helpers
   * shared/common functionality
   * tests
3. Identify architectural problems, including:

   * fat controllers
   * business logic inside controllers
   * database access inside controllers/services
   * duplicated logic
   * tightly coupled classes
   * God classes
   * inappropriate static dependencies
   * duplicated DTO/model mapping
   * inconsistent naming
   * inconsistent error handling
   * inconsistent validation
   * circular dependencies
   * poor separation of concerns
   * unnecessary abstractions
   * dead/unused code
   * inefficient database access
   * N+1 queries
   * unnecessary database calls
   * duplicated queries
   * transaction problems
   * poor async usage
   * leaking persistence concerns into higher layers

Do not immediately start moving files.

First understand how the existing system actually works and identify the dependencies between features.

---

# 2. Target Architecture

Convert the project from a traditional technical-layered monolith into a:

> **Feature-Based Monolith + 3 Application Layers**

Organize the code primarily around **business features/domains**, rather than putting all controllers in one folder, all services in another folder, etc.

The desired conceptual structure is:

```text
Application
│
├── FeatureA
│   ├── Controllers
│   ├── Services
│   ├── Repositories
│   ├── Models
│   ├── DTOs
│   ├── Validators
│   └── Mappings
│
├── FeatureB
│   ├── Controllers
│   ├── Services
│   ├── Repositories
│   ├── Models
│   ├── DTOs
│   ├── Validators
│   └── Mappings
│
├── FeatureC
│   ├── Controllers
│   ├── Services
│   ├── Repositories
│   ├── Models
│   ├── DTOs
│   ├── Validators
│   └── Mappings
│
└── Shared
    ├── Exceptions
    ├── Constants
    ├── Extensions
    ├── Utilities
    └── Infrastructure
```

Adapt the exact folder/project structure to the existing ASP.NET MVC version and project constraints.

Do **not** blindly apply a template.

The final architecture should be appropriate for the actual project.

---

# 3. Three-Layer Responsibility Model

Strictly enforce these responsibilities.

## Controller Layer

Controllers should be extremely thin.

Controllers should:

* receive HTTP requests
* perform appropriate request/model binding
* invoke the appropriate service
* return the appropriate HTTP/MVC response
* handle presentation-specific concerns

Controllers should NOT:

* contain business logic
* directly access the database
* contain SQL/ORM queries
* instantiate repositories manually
* contain complex workflows
* contain domain rules
* perform business calculations
* contain duplicated validation logic

Example:

```csharp
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<ActionResult> Details(int id)
    {
        var order = await _orderService.GetByIdAsync(id);

        if (order == null)
            return HttpNotFound();

        return View(order);
    }
}
```

Keep controllers focused on HTTP/presentation concerns.

---

# 4. Service Layer

Services contain application/business orchestration.

A service should:

* execute use cases
* coordinate repositories
* apply business rules
* perform business workflows
* coordinate transactions where appropriate
* map between application models and persistence models where necessary
* enforce application-level invariants
* provide a clear API for controllers

Example:

```csharp
public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(int id);
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
    Task UpdateAsync(UpdateOrderRequest request);
    Task DeleteAsync(int id);
}
```

Implementation:

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> GetByIdAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            return null;

        return MapToDto(order);
    }
}
```

Services should not contain infrastructure-specific details when those details belong in repositories.

Avoid creating unnecessary services such as:

```text
UserHelperService
CommonService
UtilityService
DataService
GenericBusinessService
```

unless they represent a meaningful application/domain responsibility.

---

# 5. Repository Layer

Repositories own persistence and data-access concerns.

Repositories should:

* query the database
* persist entities
* perform database-specific operations
* encapsulate ORM/database implementation details
* provide meaningful data-access operations

Repositories should NOT:

* contain HTTP concerns
* return ActionResults
* know about controllers
* contain UI logic
* implement application workflows
* contain unrelated business logic

Prefer feature-specific repositories where appropriate:

```text
OrderRepository
CustomerRepository
ProductRepository
InvoiceRepository
```

instead of forcing everything through one giant generic repository.

For example:

```csharp
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
}
```

Avoid abstractions that exist only because they look architecturally correct.

Every abstraction must provide real value.

---

# 6. Feature-Based Organization

The most important structural change is to organize the application around business capabilities/features.

For example, instead of:

```text
Controllers/
    CustomerController.cs
    OrderController.cs
    ProductController.cs

Services/
    CustomerService.cs
    OrderService.cs
    ProductService.cs

Repositories/
    CustomerRepository.cs
    OrderRepository.cs
    ProductRepository.cs

Models/
    Customer.cs
    Order.cs
    Product.cs
```

prefer something conceptually similar to:

```text
Features/
├── Customers/
│   ├── Controllers/
│   │   └── CustomersController.cs
│   ├── Services/
│   │   ├── ICustomerService.cs
│   │   └── CustomerService.cs
│   ├── Repositories/
│   │   ├── ICustomerRepository.cs
│   │   └── CustomerRepository.cs
│   ├── Models/
│   ├── DTOs/
│   └── Validators/
│
├── Orders/
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   ├── DTOs/
│   └── Validators/
│
└── Products/
    ├── Controllers/
    ├── Services/
    ├── Repositories/
    ├── Models/
    ├── DTOs/
    └── Validators/
```

Determine the actual features from the existing project.

**Do not invent arbitrary features.**

Use the existing business domains, workflows, modules, controllers, and functionality to determine appropriate feature boundaries.

---

# 7. Apply SOLID Principles

Apply SOLID rigorously but pragmatically.

### Single Responsibility

Each class should have one clear responsibility.

Split large classes where necessary.

### Open/Closed

Design components so that adding behavior does not require modifying unrelated functionality.

### Liskov Substitution

Ensure interfaces and implementations have valid substitutability.

### Interface Segregation

Avoid massive interfaces such as:

```csharp
IGeneralService
ICommonRepository
IDataService
```

Prefer focused interfaces.

### Dependency Inversion

Higher-level application logic should depend on abstractions rather than concrete infrastructure implementations.

Use dependency injection consistently.

---

# 8. Apply Clean Code Principles

Improve:

* naming
* method size
* class size
* cohesion
* readability
* duplication
* error handling
* null handling
* dependency management
* control flow
* exception handling
* comments

Prefer code that is self-explanatory.

Avoid unnecessary comments that simply describe obvious code.

Do not over-engineer the application.

Do not introduce patterns merely for the sake of using patterns.

---

# 9. Apply DDD Principles Pragmatically

Use Domain-Driven Design where it provides real value.

Identify:

* bounded business areas
* aggregates
* entities
* value objects
* domain services where appropriate
* domain rules
* domain invariants
* domain events if genuinely required

Do not force academic DDD patterns into simple CRUD functionality.

The goal is a maintainable business-oriented monolith, not a framework showcase.

Keep domain/business concepts independent from infrastructure wherever practical.

---

# 10. Preserve Existing Behavior

This is the highest-priority requirement.

The refactoring MUST NOT unintentionally change:

* public APIs
* API routes
* MVC routes
* controller/action names
* HTTP verbs
* request parameters
* response structures
* status codes
* authentication behavior
* authorization behavior
* validation behavior
* business rules
* calculation logic
* database behavior
* transaction semantics
* serialization behavior
* model binding behavior
* views
* view models
* external integrations
* configuration behavior
* scheduled/background operations
* logging behavior where externally relied upon

If an existing public class/member/API must be changed internally, preserve the externally visible contract.

If there is uncertainty about whether a change alters behavior, **do not make the behavioral change**.

Architecture should change.

Behavior should not.

---

# 11. Preserve Existing APIs

Treat existing APIs as contracts.

Before refactoring, document:

```text
Endpoint
HTTP Method
Route
Request
Response
Status Codes
Authentication
Authorization
Validation
Side Effects
```

After refactoring, verify that these remain unchanged.

Do not rename public endpoints merely to make the architecture cleaner.

Do not change route naming conventions unless absolutely necessary.

Do not change response DTO structures.

Do not introduce breaking changes.

---

# 12. Database and Repository Refactoring

Analyze all database access carefully.

Look for:

* duplicated queries
* unnecessary queries
* N+1 queries
* loading entire tables unnecessarily
* inefficient joins
* unnecessary tracking
* repeated database calls
* missing async operations
* incorrect transaction boundaries
* database calls inside loops
* unnecessary materialization
* inefficient filtering

Improve performance **only when behavior remains equivalent**.

Do not change database schema unless absolutely necessary.

Do not change query semantics merely to make code look cleaner.

Avoid premature optimization.

---

# 13. Dependency Injection

Use dependency injection consistently.

Avoid:

```csharp
var service = new OrderService();
```

inside controllers or other high-level components.

Prefer:

```csharp
private readonly IOrderService _orderService;

public OrdersController(IOrderService orderService)
{
    _orderService = orderService;
}
```

Register dependencies using the DI mechanism appropriate for the project's actual ASP.NET MVC/.NET version.

Do not introduce a new dependency injection framework if the existing application already has a suitable mechanism unless there is a compelling reason.

---

# 14. Async/Await

Review asynchronous code carefully.

Where database or I/O operations are already asynchronous, preserve that behavior.

Where appropriate, use asynchronous APIs for I/O-bound operations.

Avoid:

```csharp
.Result
.Wait()
.GetAwaiter().GetResult()
```

when they can cause blocking or deadlocks.

Do not convert everything to async mechanically.

Use async where it is technically appropriate for the actual ASP.NET MVC version and existing architecture.

---

# 15. Shared Code

Create a shared/common area only for genuinely cross-cutting concerns.

Examples:

```text
Shared/
├── Exceptions/
├── Constants/
├── Extensions/
├── Security/
├── Logging/
└── Utilities/
```

Do NOT use `Shared` as a dumping ground.

Avoid:

```text
Shared/
    Everything.cs
    CommonHelper.cs
    UtilityHelper.cs
    GlobalService.cs
```

If functionality belongs to a specific feature, keep it inside that feature.

---

# 16. Naming and Consistency

Establish and enforce consistent conventions for:

* namespaces
* classes
* interfaces
* methods
* properties
* variables
* DTOs
* request models
* response models
* repositories
* services
* controllers
* async methods
* folders
* files

Examples:

```text
IOrderService
OrderService

IOrderRepository
OrderRepository

OrdersController
OrderDto
CreateOrderRequest
UpdateOrderRequest
```

Use the naming conventions appropriate for the existing .NET codebase.

Do not introduce inconsistent naming styles.

---

# 17. Error Handling

Centralize error handling where appropriate for the project's ASP.NET MVC architecture.

Avoid duplicated:

```csharp
try
{
}
catch (Exception ex)
{
    ...
}
```

throughout every controller/service.

Do not swallow exceptions.

Do not catch exceptions unless there is a meaningful reason to handle or translate them.

Preserve existing externally observable error behavior unless the current implementation is clearly broken and changing it is explicitly required.

---

# 18. Validation

Separate responsibilities appropriately:

```text
Controller
    ↓
Request/model binding
    ↓
Service
    ↓
Business validation
    ↓
Repository
    ↓
Database
```

Keep HTTP/model validation separate from business/domain rules where practical.

Do not duplicate the same validation in multiple layers.

Preserve existing validation behavior.

---

# 19. Mapping

Review mapping between:

```text
Entity
DTO
ViewModel
Request Model
Response Model
```

Avoid exposing persistence entities directly when doing so creates unnecessary coupling.

However, do not introduce DTOs everywhere simply for architectural purity.

Use DTOs/ViewModels where they provide clear separation or are already part of the application's API/UI contract.

---

# 20. Remove Technical Debt Carefully

Identify and remove:

* dead code
* duplicate code
* obsolete classes
* unused dependencies
* unnecessary wrappers
* redundant abstractions
* unreachable code
* duplicated constants
* duplicated business rules

But be conservative.

Before deleting anything, verify that it is genuinely unused.

Do not remove code simply because it appears unused without checking references, reflection, configuration, dependency injection, routing, serialization, or framework conventions.

---

# 21. Testing and Verification

Before and after refactoring, verify functionality.

If tests already exist:

1. Run the existing test suite.
2. Fix any issues caused by the refactoring.
3. Keep all existing tests passing.

If tests are missing for important business logic:

Add focused tests where practical, especially around:

* services
* business rules
* repositories
* critical workflows
* API behavior

Prioritize tests that help prove behavior was preserved.

Do not rewrite the entire test suite unnecessarily.

---

# 22. Refactoring Strategy

Perform the work incrementally.

Use this sequence:

### Phase 1 — Understand

Analyze the entire application.

Create a map of:

```text
Feature
    ↓
Controller
    ↓
Business Logic
    ↓
Data Access
    ↓
Database
```

### Phase 2 — Identify Features

Determine logical business features from the existing application.

### Phase 3 — Establish Boundaries

Determine what belongs to:

```text
Controller
Service
Repository
Domain
Shared
```

### Phase 4 — Refactor

Move and restructure code while preserving behavior.

### Phase 5 — Simplify

Remove unnecessary duplication and abstractions.

### Phase 6 — Optimize

Optimize obvious performance problems without changing behavior.

### Phase 7 — Validate

Build and test the complete application.

### Phase 8 — Final Review

Review the resulting architecture for:

* consistency
* SOLID compliance
* clean code
* DDD alignment
* maintainability
* performance
* dependency direction
* feature boundaries
* accidental duplication
* accidental behavioral changes

---

# 23. Important Architectural Rules

Follow these rules throughout the refactoring:

### Rule 1

**Controller → Service → Repository**

The normal dependency direction should be:

```text
Controller
    ↓
Service
    ↓
Repository
    ↓
Database
```

Avoid:

```text
Controller → Repository
Controller → Database
Repository → Controller
Repository → Service
```

### Rule 2

A controller must not contain business logic.

### Rule 3

A repository must not contain application workflows.

### Rule 4

A service must not know about HTTP/MVC response types.

Avoid:

```csharp
ActionResult
JsonResult
ViewResult
HttpStatusCode
```

inside the service layer unless required by an unavoidable existing architectural constraint.

### Rule 5

Repositories must not know about controllers.

### Rule 6

Features should be as independent as reasonably possible.

### Rule 7

Avoid circular dependencies.

### Rule 8

Do not create abstractions without a meaningful reason.

### Rule 9

Do not introduce unnecessary design patterns.

### Rule 10

Prefer simple, readable code over clever code.

---

# 24. Do Not Over-Engineer

This is extremely important.

Do NOT automatically introduce:

* CQRS
* MediatR
* event sourcing
* microservices
* generic repositories
* unit-of-work abstractions
* specification patterns
* excessive factories
* excessive interfaces
* command/query handlers
* elaborate dependency frameworks

unless the existing application genuinely requires them.

The target is a:

> **Clean, maintainable, feature-based monolith with Controller → Service → Repository layers.**

Keep the architecture understandable to an ordinary .NET developer.

---

# 25. Preserve Compatibility

If the existing project has architectural constraints caused by its specific ASP.NET MVC/.NET version, adapt the proposed architecture to those constraints.

First determine whether the project uses:

* ASP.NET MVC 5 / .NET Framework
* ASP.NET Core MVC
* another MVC-compatible .NET configuration

Do not assume APIs from another .NET version.

Use the project's actual framework and conventions.

---

# 26. Deliverables

After completing the refactoring, provide a concise final report containing:

## A. Architecture Before

Explain the major problems in the original architecture.

## B. Architecture After

Show the final folder/project structure.

For example:

```text
Features/
├── Customers/
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   └── DTOs/
│
├── Orders/
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   └── DTOs/
│
└── Shared/
```

Use the **actual resulting structure**, not a generic example.

## C. Feature Map

Document the discovered features and their responsibilities.

## D. Major Refactorings

List the important changes made.

Example:

```text
OrdersController
    ↓
IOrderService
    ↓
OrderService
    ↓
IOrderRepository
    ↓
OrderRepository
    ↓
Database
```

## E. SOLID Improvements

Explain the important SOLID improvements.

## F. DDD Improvements

Explain the meaningful DDD concepts introduced or clarified.

## G. Performance Improvements

List only real performance improvements that were made.

## H. Removed Technical Debt

List meaningful dead code, duplication, or unnecessary coupling removed.

## I. Compatibility Verification

Explicitly state what was checked to ensure:

* APIs remain compatible
* routes remain compatible
* behavior remains compatible
* database behavior remains compatible
* authentication/authorization remains compatible
* existing tests pass
* application builds successfully

## J. Remaining Issues

If something could not safely be refactored, document it instead of making a risky change.

---

# 27. Quality Gate

Before considering the work complete, verify all of the following:

* [ ] Project builds successfully
* [ ] Existing tests pass
* [ ] Existing APIs remain compatible
* [ ] Existing routes remain compatible
* [ ] Controllers are thin
* [ ] Business logic is in appropriate services/domain components
* [ ] Database access is isolated in repositories
* [ ] Dependency injection is used appropriately
* [ ] No unnecessary circular dependencies exist
* [ ] Feature boundaries are logical
* [ ] Naming is consistent
* [ ] Duplicate logic is minimized
* [ ] Unnecessary abstractions are removed
* [ ] SOLID principles are followed pragmatically
* [ ] DDD concepts are applied where useful
* [ ] Database behavior is preserved
* [ ] Authentication/authorization behavior is preserved
* [ ] Validation behavior is preserved
* [ ] Error behavior is preserved
* [ ] External integrations continue to work
* [ ] No unnecessary framework/dependency changes were introduced
* [ ] No breaking API changes were introduced
* [ ] No behavior was changed merely for architectural preference

---

# Final Instruction

**Do not treat this as a greenfield rewrite.**

Treat the attached project as a production system that must retain its existing behavior.

Your priority order is:

1. **Preserve existing behavior and APIs**
2. **Preserve data and integration behavior**
3. **Establish clear feature boundaries**
4. **Separate Controller → Service → Repository responsibilities**
5. **Apply SOLID and Clean Code**
6. **Apply pragmatic DDD**
7. **Remove duplication and technical debt**
8. **Improve performance where safely possible**
9. **Improve consistency and maintainability**

When architectural cleanliness conflicts with behavior preservation, **behavior preservation wins**.

Do not make speculative changes.

Do not rewrite working business logic unnecessarily.

Do not change public contracts just to make the architecture look cleaner.

**Analyze first, plan the refactoring, then execute it carefully across the entire attached project.**
