# Benefits API

A thin RESTful API for the Benefits Engineering take-home test, built with .NET 10.

## Architecture

Clean Architecture, with the following layers:

| Project                          | Responsibility                                                                                    |
| -------------------------------- | ------------------------------------------------------------------------------------------------- |
| `src/Benefits.Domain`            | Domain entities and rules. No dependencies on other layers.                                       |
| `src/Benefits.Application`       | Use cases and application services (e.g. `IGreetingService`). Depends only on Domain.             |
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

## Hello World endpoints

- `GET /api/helloworld` -> `"Hello, World!"`

## Running tests

```bash
dotnet test
```

## Feature implementation

The target user story — adding a new starter (`EmployeeNo`, `Title`, `Firstname`,
`Surname`, `DOB`, `Gender`, `Email`, `Address` with generated unique `EmployeeNo`
and unique `Email`) — is not yet implemented; this repo currently contains the
scaffold and a hello-world endpoint only.
