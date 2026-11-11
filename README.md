# Seat Hold Admission System

Online school admission with seat holds. A parent applies for a seat, the seat is held for a limited time, and the admission is confirmed when the fee is paid.

Backend: C# (.NET 10) + PostgreSQL. Frontend: React + TypeScript (Vite). Everything runs with Docker.

## Run

Only Docker is needed.

```bash
docker compose up --build
```

| What | URL |
|---|---|
| Frontend (UI) | http://localhost:5174 |
| Backend API | http://localhost:8080 |
| PostgreSQL (host port) | localhost:5433 |

- Stop: `docker compose down`. Data stays in the `pgdata` volume, so it survives `down` and `up`.
- Delete data too: `docker compose down -v`.
- If a host port is already used on your machine, change it in `docker-compose.yml` (for example `5174:80` for the frontend).

## How to use the UI

1. **School Admin** tab: create a class (name and number of seats). The same tab shows seat counts and the list of applicants (birth registration number, class, status, transaction number). A simple password gate protects this tab; the demo password is shown on the login screen.
2. **Parent (অভিভাবক)** tab: enter the 17-digit birth registration number, press apply on a class, then pay the fee with a transaction number, or cancel.
3. A held seat expires after `HOLD_DURATION_SECONDS`. The countdown is shown, and when it reaches zero the application becomes EXPIRED and the pay button is disabled, with no page reload.
4. Seat counts refresh every few seconds. Refreshing the page or closing the browser keeps the student number and the student's applications.

## Configuration

| Variable | Default | Meaning |
|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | How long a hold lasts |
| `ConnectionStrings__Default` | set in `docker-compose.yml` | Database connection |
| `VITE_API_URL` | `http://localhost:8080` | Backend URL used by the frontend (build argument) |
| `ONE_APPLICATION_PER_STUDENT` | `true` | One active application per birth number across all classes. `false` means one per class only |
| `ADMIN_PASSWORD` | demo default | Password for the School Admin screen and `GET /admin/holds`. The endpoints from the brief never need it |

## Tests

```bash
docker compose --profile test run --rm --build tests
```

Runs the backend integration tests against a separate `seats_test` database, including a concurrency test: 40 parallel requests for 5 seats must produce exactly 5 holds and 35 `409` responses.

## API

Base URL `http://localhost:8080`, JSON. The student is identified by the `X-Student-Id` header (17-digit birth registration number).

| Method | Path | Purpose |
|---|---|---|
| POST | `/classes` | Create a class |
| GET | `/classes` | List classes with seat counts |
| GET | `/classes/{classId}` | One class with seat counts |
| POST | `/classes/{classId}/holds` | Apply (hold a seat). `409` if no seat |
| GET | `/holds/{holdId}` | Current status of an application |
| POST | `/holds/{holdId}/confirm` | Pay the fee and confirm. Optional body: `{ "transactionId": "..." }` |
| DELETE | `/holds/{holdId}` | Cancel (`204`) |
| GET | `/me/holds` | All applications of this student |

Extra endpoints used by the admin screen: `GET /admin/status`, `POST /admin/login`, `GET /admin/holds`.

## Project layout

```
backend/SeatHold.Api         ASP.NET Core Web API, EF Core, migrations
backend/SeatHold.Api.Tests   xUnit integration tests
frontend/                    React + Vite UI (served by nginx in Docker)
docker-compose.yml           db + api + frontend (+ tests with --profile test)
```

## More

- `ASSUMPTIONS.md`: decisions where the brief was silent, and why.
- `AI_USAGE.md`: which AI tool was used, where it was wrong, and how I caught it.