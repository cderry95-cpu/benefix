# Benefits API

[![CI](https://github.com/cderry95-cpu/benefix/actions/workflows/ci.yml/badge.svg)](https://github.com/cderry95-cpu/benefix/actions/workflows/ci.yml)

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

### Docker

```bash
docker build -t benefix-api .
docker run -p 8080:8080 benefix-api
```

- API listens on `http://localhost:8080`
- Multi-stage build (SDK stage compiles, trimmed ASP.NET runtime stage runs it) running as a non-root user
- Behind a corporate TLS-intercepting proxy (untrusted NuGet certificates in the build stage), pass a root CA PEM as a build secret:

```bash
docker build -t benefix-api --secret id=corp-root,src=<path-to-root-ca.pem> .
```

## Endpoints

- `GET /api/employees` — lists employees alphabetically by surname, paginated.
  Query params: `page` (default 1), `pageSize` (default 10, max 100). Response
  envelope: `{ items, page, pageSize, totalCount }`. A `page` beyond the end
  returns an empty `items` array with correct metadata; invalid params return
  a 400 ProblemDetails.
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

## Development seeding

When running locally (`Development` environment), the API seeds 25 realistic
employees on startup if the table is empty, so pagination has real data to
page through. Because the SQLite database is in memory, data resets on every
restart and the seed runs again; production deployments never seed.

## Running tests

```bash
dotnet test
```

## CI

`.github/workflows/ci.yml` runs on every push/PR targeting `main`:

1. **Build & test** — restore, build, `dotnet format --verify-no-changes`, `dotnet test`; the "Build & test" check must pass before a PR can merge.
2. **Secret scan** — gitleaks over the full git history ("Secret scan" check).

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
`Address`). `EmployeeNo` is `EMP-` plus six random zero-padded digits
(e.g. `EMP-482913`) — human-readable and unique (existence-check retry
backed by the database primary key), deliberately non-sequential so the
number leaks nothing about headcount growth. `Email` is unique; uniqueness
is enforced by a unique index in the in-memory SQLite database and checked
explicitly by the Application layer.
