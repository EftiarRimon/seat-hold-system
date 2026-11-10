# Seat Hold Admission System

A class admission system where a student places a time-limited hold on a seat, pays the admission fee, and confirms. Seats are never oversold, even under concurrent requests.

**Stack:** .NET 8 Web API, PostgreSQL, React, Docker Compose.

## Quick start

Requirements: Docker Desktop.

```bash
git clone https://github.com/EftiarRimon/seat-hold-system.git
cd seat-hold-system
docker compose up -d --build
```

| Service  | URL                    |
|----------|------------------------|
| Frontend | http://localhost:5174  |
| API      | http://localhost:TODO_API_PORT |
| Postgres | localhost:TODO_DB_PORT (data persisted in a Docker volume) |

Migrations run automatically when the API starts. Data survives `docker compose down`; use `docker compose down -v` to wipe it.

## Configuration

| Variable                | Default | Purpose |
|-------------------------|---------|---------|
| `HOLD_DURATION_SECONDS` | `600`   | How long a hold lasts before it expires |
| `ADMIN_PASSWORD`        | (unset) | Password for the School Admin tab. If empty, admin login is disabled |
| `ConnectionStrings__Default` | set in `docker-compose.yml` | PostgreSQL connection string |

## How it works

- **No overselling.** Creating a hold runs in one transaction: the class row is locked with `SELECT ... FOR UPDATE`, active holds plus confirmed seats are counted, and a hold is inserted only if the count is below `totalSeats`. Otherwise the API returns `409`. Because the lock lives in PostgreSQL, it also works with several API processes.
- **Expiry without a background job.** Each hold stores `expiresAt`. A `HELD` hold with `expiresAt < now` is treated as `EXPIRED` whenever it is read or counted, so its seat is freed automatically.
- **One application per student.** A student can have only one active (`HELD` or `CONFIRMED`) application across all classes, enforced by a transaction check and a unique index.
- **Idempotent confirm.** Confirming an already `CONFIRMED` hold returns `200` and changes nothing. Only an unexpired hold owned by the same student can be confirmed.
- **Payment.** Confirming requires a transaction number; the UI keeps the pay button disabled until one is entered.

## API

All student endpoints require the `X-Student-Id` header (17 digits).

> TODO: verify these routes against your controllers and fix any that differ.

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/classes` | List classes with seats left |
| POST   | `/classes` | Create a class (admin) |
| DELETE | `/classes/{id}` | Delete a class (admin) |
| POST   | `/classes/{id}/holds` | Hold a seat |
| POST   | `/holds/{id}/confirm` | Confirm a hold with a transaction number |
| DELETE | `/holds/{id}` | Cancel a hold |
| GET    | `/admin/applicants` | List applicants (admin) |

Status codes: `400` validation, `401` missing/invalid identity or admin login, `403` hold belongs to another student, `404` not found, `409` no seats left / duplicate application / invalid state.

## Frontend

- Student ID is entered at the top (17 digits, validated) and saved in `localStorage`.
- Classes are polled every few seconds.
- A countdown runs from `expiresAt`; at zero the hold shows `EXPIRED` and the pay button is disabled.
- A **School Admin** tab manages classes and shows the applicant list.

## Tests

Requires the .NET 8 SDK and a PostgreSQL instance on the test connection string (default `Host=localhost;Port=5433;Database=seats_test;Username=app;Password=app`, overridable with `TEST_DB`).

```bash
dotnet test backend/SeatHold.Api.Tests
```

Tests include a concurrency test that fires many parallel holds at one class and asserts `held + confirmed <= totalSeats`. The test factory forces `ADMIN_PASSWORD` empty so results do not depend on your shell.

## Project layout

```
backend/    .NET 8 API, migrations, tests
frontend/   React app
docker-compose.yml
README.md  ASSUMPTIONS.md  AI_USAGE.md
```