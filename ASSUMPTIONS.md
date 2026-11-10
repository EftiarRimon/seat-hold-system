# Assumptions

Decisions made where the brief was silent.

## Identity and errors

- Students identify themselves with an `X-Student-Id` header containing exactly 17 digits. No real authentication is implemented.
- Validation errors return `400`. A missing or malformed `X-Student-Id` returns `401`.
- A hold owned by another student returns `403`. A non-existent hold or class returns `404`.
- No seats left, a duplicate application, or an invalid state change returns `409`.

## Classes

- Class name must be non-empty (trimmed). `totalSeats` must be an integer greater than 0.
- Creating and deleting classes is restricted to the School Admin, protected by `ADMIN_PASSWORD`. If the variable is empty, admin login is disabled.

## Holds

- Hold length is `HOLD_DURATION_SECONDS` (default 600 seconds).
- Expiry is computed on read from `expiresAt`; no background job is required for correctness.
- Expired and cancelled holds are kept as history, not deleted. They do not count against seats.
- Confirming an already confirmed hold is idempotent and returns `200`.
- Deleting a `CONFIRMED` hold is rejected with `409`, because a paid admission should not be silently undone by a student. (Change this line if your code allows it.)

## Applications and payment

- A student may have only one active (`HELD` or `CONFIRMED`) application across all classes. Applying to a second class returns `409`.
- A transaction number is required to confirm. No real payment gateway is called; the number is stored as entered and only checked for being non-empty. (Add any format rule you enforce.)

## Other

- Times are stored and compared in UTC.
- The UI polls every few seconds instead of using WebSockets, to keep the system simple.