## 📁 Proyecto: `AccessControl`

```
AccessControl.sln
│
├── 📁 AccessControl.API             # Proyecto de presentación (Web API Controllers)
│   ├── Program.cs
│   ├── appsettings.json
│   └── Controllers
│       └── UsersController.cs
│       └── AccessLogsController.cs
│
├── 📁 AccessControl.Application     # Lógica de negocio, interfaces y DTOs
│   ├── Interfaces
│   │   └── IUserService.cs
│   │   └── IAccessLogService.cs
│   ├── Services
│   │   └── UserService.cs
│   │   └── AccessLogService.cs
│   └── DTOs
│       └── UserDto.cs
│       └── ResidentDto.cs
│       └── AccessLogDto.cs
│
├── 📁 AccessControl.Domain          # Entidades del dominio (solo modelos)
│   └── Entities
│       └── UserBase.cs
│       └── Resident.cs
│       └── AccessLog.cs
│       └── Employee.cs
│       └── Visitor.cs
│       └── Supplier.cs
│
├── 📁 AccessControl.Infrastructure  # Implementaciones de servicios externos (opcional)
│   └── Services
│       └── EmailNotifier.cs
│       └── ExternalAuthValidator.cs
│
├── 📁 AccessControl.Persistence     # Acceso a datos (EF Core, DbContext, repos)
│   ├── ApplicationDbContext.cs
│   ├── Repositories
│   │   └── UserRepository.cs
│   │   └── AccessLogRepository.cs
│   ├── Interfaces
│   │   └── IUserRepository.cs
│   │   └── IAccessLogRepository.cs
│   └── Migrations
│       └── [EF Migration Files Here]
│
└── 📁 AccessControl.Tests           # Proyecto de pruebas unitarias
    └── UserServiceTests.cs
    └── AccessLogServiceTests.cs
```

---

### 🧩 Relación entre proyectos

* `AccessControl.API` **depende** de:

  * `Application` (para servicios)
  * `DTOs` (para input/output de datos)

* `Application` **depende** de:

  * `Domain` (para usar entidades)
  * `Persistence` (solo a través de interfaces)

* `Persistence` **depende** de:

  * `Domain` (para conocer las entidades)

---
### 🔐 Security Note: Managing Secrets

**Do not store sensitive data like database passwords or JWT keys directly in configuration files.** This project is configured to use the .NET Secret Manager for development.

To configure your local environment, run the following commands from the `backend/AccessControl.API` directory:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ep-mute-cherry-a4j6mcyc-pooler.us-east-1.aws.neon.tech;Database=verceldb;Username=default;Password=<YOUR_ACTUAL_PASSWORD>;Ssl Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:Key" "<YOUR_SUPER_SECRET_JWT_KEY>"
```

Replace `<YOUR_ACTUAL_PASSWORD>` and `<YOUR_SUPER_SECRET_JWT_KEY>` with your actual credentials.

### 👤 User Creation

Users can be created via the API. The `POST /api/auth/register` endpoint is protected and requires an `Admin` user's JWT for authorization.

**Endpoint:** `POST https://localhost:7262/api/auth/register`

**Headers:**
- `Content-Type: application/json`
- `Authorization: Bearer <ADMIN_JWT_TOKEN>`

**Request Body:**

```json
{
  "email": "user@example.com",
  "fullName": "Full Name",
  "password": "AStrongPassword123!",
  "role": <ROLE_ID>
}
```

**Role IDs:**
- `0`: Admin
- `1`: Guard
- `2`: Owner

**cURL Examples:**

*   **Create an Admin user:**
    ```bash
    curl -X POST https://localhost:7262/api/auth/register \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
    -d '{
      "email": "admin@example.com",
      "fullName": "Administrator",
      "password": "AStrongPassword123!",
      "role": 0
    }'
    ```

*   **Create a Guard user:**
    ```bash
    curl -X POST https://localhost:7262/api/auth/register \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
    -d '{
      "email": "guard@example.com",
      "fullName": "Security Guard",
      "password": "AStrongPassword123!",
      "role": 1
    }'
    ```

*   **Create an Owner user:**
    ```bash
    curl -X POST https://localhost:7262/api/auth/register \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
    -d '{
      "email": "owner@example.com",
      "fullName": "John Doe",
      "password": "AStrongPassword123!",
      "role": 2
    }'
    ```

