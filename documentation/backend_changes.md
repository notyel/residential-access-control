# Backend Documentation: Person & Visit Refactoring

This document outlines the recent changes made to the backend data model and services. It serves as a guide for creating the new API controllers and implementing the necessary frontend modifications.

---

## 1. Data Model Changes

The core of the refactoring was to normalize visitor information into a dedicated `Person` entity.

### New `Person` Entity

A new entity has been created to store information about any external individual (visitors, contractors, providers, etc.).

**File:** `backend/AccessControl.Domain/Entities/Person.cs`
```csharp
using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Person : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string DocumentType { get; set; } = null!;
        public string DocumentNumber { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int PersonType { get; set; } // Enum or lookup table could be used here
    }
}
```
**Database Constraint:** The `DocumentNumber` has a unique index in the database to prevent duplicate person records.

### Modified `Visit` Entity

The `Visit` entity no longer contains `VisitorName` and `VisitorId`. Instead, it holds a foreign key reference (`PersonId`) to the `Person` entity.

**File:** `backend/AccessControl.Domain/Entities/Visit.cs`
```csharp
using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Visit : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PersonId { get; set; } // Foreign Key to Person
        public Person Person { get; set; } = null!; // Navigation Property
        public string? VehiclePlate { get; set; }
        public DateTime? CheckOut { get; set; }
        public Guid ResidenceId { get; set; }
        public Residence Residence { get; set; } = null!;
        public Guid RegisteredById { get; set; }
        public User RegisteredBy { get; set; } = null!;
    }
}
```

---

## 2. Data Transfer Object (DTO) Changes

The DTOs used for API communication have been updated to reflect the new data model.

### Person DTOs

**`PersonDto.cs`** (For sending person data to the client)
```csharp
public class PersonDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string DocumentNumber { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int PersonType { get; set; }
}
```

**`CreatePersonDto.cs`** (For receiving data to create a new person)
```csharp
public class CreatePersonDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string DocumentNumber { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int PersonType { get; set; }
}
```

### Visit DTOs

**`VisitDto.cs`** (For sending visit details to the client)
```csharp
public class VisitDto
{
    public Guid Id { get; set; }
    public PersonDto Person { get; set; } = null!; // Contains the full person object
    public string? VehiclePlate { get; set; }
    public DateTime? CheckOut { get; set; }
    public Guid ResidenceId { get; set; }
    public string? ResidenceIdentifier { get; set; }
    public Guid RegisteredById { get; set; }
    public string? RegisteredByFullName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**`CreateVisitDto.cs`** (For receiving data to register a new visit)
```csharp
public class CreateVisitDto
{
    // To link an EXISTING person, provide their ID.
    public Guid? PersonId { get; set; }

    // To create a NEW person with the visit, provide their details.
    public CreatePersonDto? NewPerson { get; set; }

    public string? VehiclePlate { get; set; }
    public Guid ResidenceId { get; set; }
}
```
*Logic:* The backend service will first check for `PersonId`. If it's null, it will then check for `NewPerson` and create a new person record.

**`UpdateVisitDto.cs`** (For receiving data to update a visit)
```csharp
public class UpdateVisitDto
{
    // The person associated with the visit can be changed.
    public Guid PersonId { get; set; }
    public string? VehiclePlate { get; set; }
}
```

---

## 3. API Controller Guide (Conceptual)

While the controllers have not been created yet, this section provides a roadmap for their implementation based on the service layer changes.

### New `PersonsController`

A new controller is required to manage `Person` entities independently.

**Endpoints:**

*   **`GET /api/persons`**
    *   **Purpose:** Search for existing persons. The most common use case will be searching by `documentNumber` to check if a person already exists before registering a visit.
    *   **Query Parameter:** `?documentNumber=...`
    *   **Response:** `ResponseModel<IEnumerable<PersonDto>>`

*   **`POST /api/persons`**
    *   **Purpose:** Create a new person record without registering a visit.
    *   **Request Body:** `CreatePersonDto`
    *   **Response:** `ResponseModel<PersonDto>`

*   **`PUT /api/persons/{id}`**
    *   **Purpose:** Update the details of an existing person.
    *   **Request Body:** A new `UpdatePersonDto` would be needed.
    *   **Response:** `ResponseModel<PersonDto>`

### Modified `VisitsController`

The existing `VisitsController` will need to be updated.

**Endpoints:**

*   **`POST /api/visits` (Register Visit)**
    *   **Purpose:** Register a new visit.
    *   **Request Body:** `CreateVisitDto`.
    *   **Frontend Logic:**
        1.  The user (guard) enters the person's document number.
        2.  The frontend calls `GET /api/persons?documentNumber=...` to check if the person exists.
        3.  **If the person exists:** The frontend sends a `POST /api/visits` request with the `personId` from the search result.
        4.  **If the person does not exist:** The frontend collects the new person's details and sends a `POST /api/visits` request with the `newPerson` object filled in.

*   **`GET /api/visits`**, **`GET /api/visits/{id}`**, etc.
    *   **Purpose:** Retrieve visit information.
    *   **Response:** The response will now nest the full `PersonDto` object within the `VisitDto`, so the frontend will need to be updated to access person details via `visit.person.firstName`, etc.
