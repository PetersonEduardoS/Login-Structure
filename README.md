# Login-Structure

API de autenticacao em ASP.NET Core, com cadastro e login de usuarios via Entity Framework Core.

## Tecnologias

- ASP.NET Core 8
- Entity Framework Core + SQLite
- Autenticacao baseada em token (JWT)

## Estrutura

- `Controllers/AuthController.cs`
- `Models/User.cs`
- `Data/`, `Migrations/`

## Como rodar

```
cd LoginAPI
dotnet ef database update
dotnet run
```

> Projeto em desenvolvimento.
