# Todo Application Backend (.NET 8 Web API)

This is the backend API for the Angular + .NET Todo Application. It is built using ASP.NET Core Web API with .NET 8.0, and includes a full-coverage automated testing suite covering unit, integration, API, and security validation.

---

## 🚀 Key Features

* **Complete Todo CRUD API**: Supports full task management including pagination, search query filtering, and completion state filtering.
* **Authentication & Authorization**: Secure User Registration and Login endpoints returning custom-configured JWT tokens.
* **Architecture Pattern**: Implements Controller-Service-Repository patterns separating data storage, business logic, and request routing.
* **Global Exception Middleware**: Maps internal exceptions (like FluentValidation `ValidationException` or `KeyNotFoundException`) into structured HTTP response models (e.g., `400 Bad Request`, `404 Not Found`, `401 Unauthorized`).
* **AutoMapper & DTOs**: Strict request/response schema mapping preventing overposting or database schema leaks.
* **FluentValidation**: Strong request validation ensuring valid names, email shapes, and password strengths before processing.

---

## 🔒 Security Implementations

* **SQL Injection (SQLi) Protection**: Uses Entity Framework Core parameterized query generation to guarantee inputs are handled as literals rather than executable SQL commands.
* **Insecure Direct Object Reference (IDOR) Protection**: Strictly validates the logged-in user's identity (`ClaimTypes.NameIdentifier`) against the target resource's ownership in all CRUD actions.
* **Overposting Attack Prevention**: Request endpoints accept strict DTO schemas (`CreateTodoDto`, `UpdateTodoDto`) rather than direct entity models. This completely blocks attempts to override auto-assigned properties (like `Id`, `UserId`, `CreatedAt`, etc.).
* **JWT Tampering Protection**: Token validation strictly verifies the issuer signature, expiration, and audience claims, automatically rejecting altered tokens on the HTTP pipeline.

---

## 🛠️ Technology Stack

* **Core**: .NET 8.0
* **ORM**: Entity Framework Core 8.x
* **Database Drivers**: Pomelo Entity Framework Core MySQL (Production) & EF Core In-Memory (Test/Local Fallback)
* **Validation**: FluentValidation 11.9
* **Mapping**: AutoMapper 13.0
* **Cryptography**: BCrypt.Net-Next (for salt-hashed password storage)
* **Testing**: xUnit, FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing

---

## 📂 Project Structure

```
todo-azure-backend/
├── Controllers/         # API Endpoint Routers (AuthController, TodoController)
├── Data/                # DbContext configurations and mappings
├── DTOs/                # Data Transfer Objects (Request/Response contracts)
├── Mapping/             # AutoMapper profiles
├── Middleware/          # Global Exception Handling Middleware
├── Models/              # Database Entities (User, Todo)
├── Repositories/        # Database Access Layer (IUserRepository, ITodoRepository)
├── Services/            # Business Logic Layer (IAuthService, ITodoService, IJwtService)
├── Validators/          # FluentValidation rules
├── Properties/          # launchSettings.json configurations
├── TodoApi.Tests/       # Test Suite (Unit, Integration, Security, and API tests)
├── Program.cs           # Application bootstrap and pipeline setup
└── TodoApi.csproj       # Project configuration & package references
```

---

## ⚙️ Configuration & Local Run

### Environment Variables (`.env`)
Create a `.env` file in the root of the backend folder containing:
```env
DB_CONNECTION_STRING=Server=localhost;Port=3306;Database=todo_db;User=root;Password=root_password;
JWT_SECRET=SuperSecretKeyForTodoAppAuthJWTToken2026
JWT_ISSUER=TodoApi
JWT_AUDIENCE=TodoUi
RUN_MIGRATIONS=true
ENABLE_SWAGGER=true
```

### Local Run without MySQL (In-Memory Database Fallback)
For development and Selenium E2E testing where running a local MySQL server is not desired, you can instruct the backend to use an isolated **In-Memory Database**:
```powershell
# In PowerShell:
$env:USE_INMEMORY_DB="true"
dotnet run --launch-profile http
```
The API will start and listen on `http://localhost:5033`.

---

## 🧪 Testing Suite

The backend contains a test suite of **83 automated tests** executing unit and API integration assertions.

### Run Tests
To execute the backend testing suite, navigate to the `TodoApi.Tests` directory and run:
```bash
dotnet test
```

### Test Coverage Breakdown
1. **Unit Tests (`TodoApi.Tests/Validators`, `/Services`, `/Middleware`, `/Repositories`)**:
   * Asserts validator rules (email validations, password strengths, required fields).
   * Mocks dependencies using `Moq` to verify business layers and repositories.
   * Asserts custom HTTP response status mappings for various exception types.
2. **Integration / API Tests (`TodoApi.Tests/Integration/`)**:
   * Bootstraps the real API environment in-memory using `CustomWebApplicationFactory` and `WebApplicationFactory`.
   * Simulates real client HTTP requests for auth workflows and Todo CRUD workflows.
3. **Security Integration Tests (`TodoApi.Tests/Integration/SecurityApiTests.cs`)**:
   * Evaluates protection against overposting, SQL Injection payloads, and signature tampering on JWT bearer headers.
