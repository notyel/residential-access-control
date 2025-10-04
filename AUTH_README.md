# Zentryal Authentication System

## Overview

This document describes the authentication system implemented for Zentryal (Residential Access Control).

## Architecture

### Frontend (Angular 18)

- **Login Component** (`front/src/app/modules/auth/components/login`)
  - Reactive form with email and password validation
  - Zentryal branding with shield icon
  - Error handling and loading states

- **Auth Service** (`front/src/app/core/services/auth.service.ts`)
  - Manages authentication state
  - Stores JWT token and user info in localStorage
  - Provides login/logout methods
  - Observable streams for token and user

- **Auth Guard** (`front/src/app/core/guards/auth.guard.ts`)
  - Protects routes from unauthorized access
  - Redirects to login if not authenticated

- **Auth Interceptor** (`front/src/app/core/interceptors/auth.interceptor.ts`)
  - Automatically adds JWT token to all HTTP requests
  - Uses Bearer authentication scheme

### Backend (.NET 8)

- **User Entity** (`backend/AccessControl.Domain/Entities/User.cs`)
  - Id, Email, PasswordHash, FullName, IsActive, CreatedAt
  - Passwords hashed using SHA256

- **Auth Controller** (`backend/AccessControl.API/Controllers/AuthController.cs`)
  - POST `/api/auth/login` - Authenticate user
  - POST `/api/auth/register` - Register new user

- **Auth Service** (`backend/AccessControl.Application/Services/AuthService.cs`)
  - Validates credentials
  - Generates JWT tokens
  - Manages user registration

- **JWT Configuration** (`backend/AccessControl.API/Program.cs`)
  - Configurable JWT key, issuer, and audience
  - Token validation on protected endpoints

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- npm

### Backend Setup

1. Navigate to backend directory:
   ```bash
   cd backend
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Apply migrations:
   ```bash
   cd AccessControl.API
   dotnet ef database update --project ../AccessControl.Persistence
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

   The API will be available at `http://localhost:5069`

### Frontend Setup

1. Navigate to frontend directory:
   ```bash
   cd front
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm start
   ```

   The application will be available at `http://localhost:4200`

## Test Account

A test user is created automatically:

- **Email:** admin@zentryal.com
- **Password:** password123

## API Endpoints

### Authentication

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@zentryal.com",
  "password": "password123"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "guid",
    "email": "admin@zentryal.com",
    "fullName": "Admin User"
  }
}
```

#### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "fullName": "John Doe"
}
```

## Security Configuration

### JWT Settings (appsettings.json)

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-chars-long",
    "Issuer": "Zentryal",
    "Audience": "Zentryal"
  }
}
```

**Important:** Change the JWT key in production to a secure random value.

### CORS

The backend is configured to allow all origins for development. Update the CORS policy in `Program.cs` for production:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production",
        builder =>
        {
            builder.WithOrigins("https://your-domain.com")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
```

## Password Security

Currently, passwords are hashed using SHA256. For production, consider using a more secure algorithm like:

- BCrypt
- Argon2
- PBKDF2

Example with BCrypt:
```csharp
// Install BCrypt.Net-Next package
using BCrypt.Net;

private static string HashPassword(string password)
{
    return BCrypt.HashPassword(password);
}

private static bool VerifyPassword(string password, string hash)
{
    return BCrypt.Verify(password, hash);
}
```

## Token Expiration

Tokens are configured to expire after 7 days. Adjust in `AuthService.cs`:

```csharp
var token = new JwtSecurityToken(
    // ... other settings
    expires: DateTime.UtcNow.AddDays(7), // Change this value
    // ...
);
```

## Troubleshooting

### CORS Errors

If you see CORS errors in the browser console:
1. Ensure backend is running on port 5069
2. Update frontend auth service API_URL if needed
3. Check CORS policy in backend Program.cs

### Database Issues

If migrations fail:
```bash
cd backend/AccessControl.API
rm accesscontrol.db*
dotnet ef database update --project ../AccessControl.Persistence
```

### Port Conflicts

Backend default: 5069 (configured in launchSettings.json)
Frontend default: 4200 (Angular default)

Change ports if needed in respective configuration files.
