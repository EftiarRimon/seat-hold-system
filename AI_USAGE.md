# AI Usage

## Tool used

Claude (Anthropic), via chat, as a pair-programming assistant.

## Where AI helped

- Proposing the overall design: row-level locking with `SELECT ... FOR UPDATE` to prevent overselling, expiry computed on read, a unique index for one active hold, and idempotent confirm.
- Drafting the backend skeleton (entities, DbContext, endpoints) and the React UI (student ID input, polling, countdown, error messages).
- Drafting tests, including the parallel-holds concurrency test.
- Debugging: for example, finding that a leftover `ADMIN_PASSWORD` in my shell made every `POST /classes` return `401` in tests, and fixing it by setting the variable empty in the test factory.
- Moving the project from .NET 10 to .NET 8 to match the brief.
- Drafting this documentation.

## What I changed or decided myself

- TODO: list requirements you added or changed (e.g. one application per student, transaction number on payment, admin applicant list).
- TODO: list any AI suggestion you rejected or altered, and why.

## How I verified the work

- Ran `dotnet test backend/SeatHold.Api.Tests` and confirmed all tests pass, including the concurrency test.
- Ran the full stack with `docker compose up` from a fresh clone.
- Manually checked in the browser: duplicate applications are rejected, the pay button stays disabled without a transaction number, holds expire at zero, and the admin tab lists applicants.
- TODO: add anything else you checked (two backend processes, restart persistence).

## Responsibility

I reviewed all AI-generated code before committing it and can explain how each part works.