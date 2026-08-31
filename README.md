# Login-Structure

Authentication API in ASP.NET Core, with user registration and login via Entity Framework Core.

## Tech

- ASP.NET Core 8
- Entity Framework Core + SQLite
- Token-based authentication (JWT)

## Structure

- `Controllers/AuthController.cs`
- `Models/User.cs`
- `Data/`, `Migrations/`

## How to run

```
cd LoginAPI
dotnet ef database update
dotnet run
```

> Work in progress.
