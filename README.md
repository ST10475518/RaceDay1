# RaceDay API (Part 2)

ASP.NET Core 8 Web API for the RaceDay system: events, categories, enrolments and results,
with session-based, role-based authentication, Swagger UI, xUnit tests and GitHub Actions CI.

## CI status
<!-- TODO: replace with your own screenshot of a green GitHub Actions run -->
![Green build](docs/green-build.png)

## Demo video
<!-- TODO: paste your unlisted YouTube link -->
https://youtu.be/REPLACE_ME

## Roles
| Role | Can do |
|------|--------|
| **Organiser** | Create, update and delete **their own** events; define categories; view all enrolments for their events; capture finish times and positions. Can also view events/categories and manage their profile. |
| **Participant** | View events and categories; enrol in an event by choosing a category; view/withdraw their own enrolments; view their own results; manage their profile. |

Organiser sign-up requires an **organiser code** (`Registration:OrganiserCode` in `appsettings.json`,
default `RACEDAY-ORG-2026`) so users can't simply promote themselves. Change it for any real deployment.

## Authentication
`POST /api/auth/login` verifies the password against a salted hash (ASP.NET `PasswordHasher`) and starts a
server-issued session: an HttpOnly cookie (`RaceDay.Session`) carrying the user id and role. All non-public
endpoints use `[Authorize]`; role-restricted endpoints use `[Authorize(Roles = ...)]`.
Unauthenticated -> **401**, wrong role / not the owner -> **403**.

## Setup & run
Prerequisites: .NET 8 SDK, SQL Server or LocalDB (Visual Studio installs LocalDB), `dotnet-ef` tool.

```bash
dotnet tool install --global dotnet-ef
dotnet restore
# Create the first migration (code-first) - run once and commit the generated Migrations folder
dotnet ef migrations add InitialCreate --project RaceDay.Api
dotnet run --project RaceDay.Api       # applies migrations to the DB on startup
```
Adjust `ConnectionStrings:DefaultConnection` in `RaceDay.Api/appsettings.json` if you're not using LocalDB.
Open the URL printed in the console + `/swagger`.

**Try it in Swagger:** register -> `POST /api/auth/login` -> then call any endpoint (the session cookie is sent automatically).

## Tests
```bash
dotnet test
```
Tests run the real API in memory (`WebApplicationFactory`) with an in-memory database; no SQL Server needed.
They cover registration/login, event management, role rejection, enrolments and results (success + failure paths).

## Endpoints
| Method | Route | Access |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Logged in |
| GET / PUT | /api/users/me | Logged in |
| GET | /api/events, /api/events/{id} | Logged in |
| POST / PUT / DELETE | /api/events, /api/events/{id} | Organiser (owner) |
| GET | /api/events/{eventId}/categories | Logged in |
| POST | /api/events/{eventId}/categories | Organiser (owner) |
| PUT / DELETE | /api/categories/{id} | Organiser (owner) |
| POST | /api/enrolments | Participant |
| GET | /api/enrolments/my | Participant |
| DELETE | /api/enrolments/{id} | Participant (own) |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) |
| POST / PUT | /api/results, /api/results/{id} | Organiser (owner) |
| GET | /api/events/{eventId}/results | Organiser (owner) |
| GET | /api/results/my | Participant |

## Project structure
```
RaceDay.Api/      Models (entities) | Data (DbContext) | Dtos | Controllers | Program.cs
RaceDay.Tests/    xUnit integration-style tests
.github/workflows/ci.yml   build + test on every push
```
