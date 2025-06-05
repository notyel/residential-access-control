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


