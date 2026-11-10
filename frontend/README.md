# Frontend

React + TypeScript + Vite UI for the Seat Hold Admission System.

See the root [README.md](../README.md) for how to run the whole stack with Docker Compose.

## Local development

```bash
npm install
npm run dev
```

The dev server proxies or calls the API configured in `.env` (see `VITE_API_URL`, if you use it).

## Build

```bash
npm run build
```

## Features

- Student ID input (17 digits, validated) saved in `localStorage`
- Class list polled every few seconds
- Countdown from `expiresAt`, with the pay button disabled at zero
- School Admin tab: manage classes and view applicants