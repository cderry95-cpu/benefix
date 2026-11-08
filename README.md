# Benefits API

A thin RESTful API for the Benefits Engineering take-home test, built with .NET 10.

## Architecture

Clean Architecture, with the following layers:

| Project                          | Responsibility                                                                                    |
| -------------------------------- | ------------------------------------------------------------------------------------------------- |
| `src/Benefits.Domain`            | Domain entities and rules. No dependencies on other layers.                                       |
| `src/Benefits.Application`       | Use cases and application services (e.g. `IAddStarterService`). Depends only on Domain.          |
| `src/Benefits.Infrastructure`    | Persistence and external concerns. Depends on Application.                                        |
| `src/Benefits.Api`               | ASP.NET Core REST API exposing Application use cases. Swagger/OpenAPI enabled in development.      |
| `tests/Benefits.Api.Tests`       | Unit and integration tests (xUnit, in-memory `WebApplicationFactory`).                             |

Dependency rule: `Api -> Application -> Domain`, with `Infrastructure -> Application`.
The Domain has no outward dependencies.

## Getting started

```bash
dotnet restore
dotnet run --project src/Benefits.Api
```

- API: `https://localhost:xxxx` / `http://localhost:5182`
- Swagger UI: `/swagger` (development only)

## Endpoints

- `POST /api/employees` — adds a new starter. See `src/Benefits.Api/Benefits.Api.http` for a sample request body.

| Case                          | Status | Response                                   |
| ----------------------------- | ------ | ------------------------------------------ |
| Starter saved                 | 200    | Saved employee (with GUID `employeeNo`)     |
| Missing `email`/`firstname`/`surname` | 400 | ProblemDetails listing the missing fields |
| Malformed `email`             | 400    | ProblemDetails                              |
| `dob` in the future           | 400    | ProblemDetails                              |
| Unknown `gender` value        | 400    | ProblemDetails listing allowed values: `Male, Female, Other, Prefer not to say` |
| `email` already registered    | 409    | ProblemDetails                              |

Required fields: `email`, `firstname`, `surname`. All other fields are optional
(`title`, `dob`, `gender`, `address`). `dob` must not be in the future.
`gender` accepts the enum value names (case-insensitive) or their spaced
forms, e.g. `"Male"`, `"prefer not to say"`.

## Running tests

```bash
dotnet test
```

## Git hooks

A pre-commit hook checks for leaked secrets (gitleaks), verifies formatting
(`dotnet format --verify-no-changes`) and builds the solution. Enable it once
after cloning:

```bash
git config core.hooksPath .githooks
```

Secret scanning works best with [gitleaks](https://gitleaks.io) installed
(`brew install gitleaks`); a basic regex fallback runs if it is absent.

## Feature implementation

Implements the user story: as an HR Administrator, add a new starter
(`EmployeeNo`, `Title`, `Firstname`, `Surname`, `DOB`, `Gender`, `Email`,
`Address`). `EmployeeNo` is a generated GUID (not sequential) and `Email`
is unique; uniqueness is enforced by a unique index in the in-memory SQLite
database and checked explicitly by the Application layer.
