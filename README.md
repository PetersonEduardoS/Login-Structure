# Login-Structure

A minimal authentication API built with ASP.NET Core 8 and Entity Framework Core. It provides user registration, JWT-based login, and role-protected admin endpoints for user management.

## Tech stack

- ASP.NET Core 8 (Web API)
- Entity Framework Core 9 + SQLite
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt.Net-Next for password hashing
- Swashbuckle (Swagger / OpenAPI) for interactive API docs

## Project structure

```
LoginAPI/
  Controllers/
    AuthController.cs      # registration, login, and user management endpoints
  Models/
    User.cs                # User entity (Id, Email, PasswordHash, IsAdmin)
  Data/
    AppDbContext.cs         # EF Core DbContext
  Migrations/                # EF Core migrations
  Program.cs                 # app startup, JWT + Swagger configuration
  appsettings.json           # non-secret configuration (Jwt issuer/audience/expiry)
```

`bin/`, `obj/`, `.vs/`, and `*.db` (the local SQLite database) are gitignored and not part of the repository.

## Prerequisites

- .NET 8 SDK
- `dotnet-ef` tool (for running migrations): `dotnet tool install --global dotnet-ef`

## Setup

All commands below run from the `LoginAPI/` folder.

### 1. Restore packages

```bash
cd LoginAPI
dotnet restore
```

### 2. Configure the JWT signing key

The API requires a secret key used to sign and validate JWTs. It is **not** stored in `appsettings.json` or committed to the repo — it must be provided locally via .NET user-secrets (development) or an environment variable (any other environment).

Generate a random 256-bit key:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

or, if you have OpenSSL:

```bash
openssl rand -base64 32
```

Store it:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "PASTE_YOUR_GENERATED_KEY_HERE"
```

In non-development environments, set the `Jwt__Key` environment variable instead (double underscore is the standard ASP.NET Core config-section separator).

The app validates this at startup and throws `InvalidOperationException` immediately if `Jwt:Key` is missing, rather than failing later at login time.

`appsettings.json` holds the non-secret JWT settings:

```json
"Jwt": {
  "Issuer": "LoginAPI",
  "Audience": "LoginAPI",
  "ExpiresMinutes": 60
}
```

### 3. Apply database migrations

```bash
dotnet ef database update
```

This creates `users.db` (SQLite) in the `LoginAPI/` folder.

### 4. Run

```bash
dotnet run
```

By default the API listens on `http://localhost:5125` (check `Properties/launchSettings.json` for the exact port). Swagger UI is available at `/swagger` in development.

## Authentication & authorization

- Passwords are hashed with BCrypt before being stored — plaintext passwords are never persisted.
- `POST /api/Auth/login` returns a signed JWT containing the user's id, email, and role (`Admin` or `User`) as claims.
- Protected endpoints require an `Authorization: Bearer <token>` header. In Swagger UI, click **Authorize** and paste the token (no need to type `Bearer` — it's added automatically).
- Admin-only endpoints are enforced server-side with `[Authorize(Roles = "Admin")]`. There is no client-supplied header or field that grants admin access — the role comes only from the `IsAdmin` value stored for that user at the time they log in.
- Tokens expire after `Jwt:ExpiresMinutes` (default 60 minutes). A user must log in again to get a fresh token, e.g. after being promoted to Admin.

## API reference

### `POST /api/Auth/register`

Public. Creates a new user with `IsAdmin = false`.

Request body:
```json
{ "email": "user@example.com", "password": "Secret123!" }
```

Responses: `200 OK` / `400 Bad Request` (email already exists).

### `POST /api/Auth/login`

Public. Validates credentials and returns a JWT.

Request body:
```json
{ "email": "user@example.com", "password": "Secret123!" }
```

Response `200 OK`:
```json
{ "token": "eyJhbGciOi...", "expiresAt": "2026-09-02T00:12:49Z" }
```

Response `400 Bad Request` on invalid credentials.

### `GET /api/Auth/all`

**Admin only.** Returns all users (`Id`, `Email`, `IsAdmin`).

### `PUT /api/Auth/{id}/email`

**Admin only.** Updates a user's email.

Request body: `{ "email": "new@example.com" }`

### `PUT /api/Auth/{id}/password`

**Admin only.** Updates a user's password (re-hashed with BCrypt).

Request body: `{ "password": "NewSecret123!" }`

### `PUT /api/Auth/{id}/role`

**Admin only.** Promotes or demotes a user.

Request body: `{ "isAdmin": true }`

This is the only supported way to grant `IsAdmin` — there is no self-service or registration-time path to becoming an admin.

### `DELETE /api/Auth/{id}`

**Admin only.** Deletes a user.

All "Admin only" endpoints return `401 Unauthorized` with no token and `403 Forbidden` with a valid token belonging to a non-admin user.

## Creating the first admin

Since `PUT /api/Auth/{id}/role` itself requires an authenticated Admin, there is a bootstrap step for the very first admin account:

1. Register a normal user via `POST /api/Auth/register`.
2. Stop the app.
3. Open `LoginAPI/users.db` with a SQLite client (e.g. DB Browser for SQLite, or a VS Code SQLite extension).
4. In the `Users` table, set `IsAdmin` to `1` for that user's row. Save.
5. Restart the app and log in again with that user — the new token will carry the `Admin` role.
6. From then on, use `PUT /api/Auth/{id}/role` (authenticated as that admin) to promote/demote other users.

## Known limitations / possible next steps

- No refresh tokens — expired tokens require a full re-login.
- No rate limiting on `login`/`register`.
- No email verification.
- No automated tests yet.
