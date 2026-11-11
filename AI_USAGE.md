# AI Usage

## Tool
Claude (Anthropic), used in a chat window for occasional assistance. I used it mainly for guidance, suggestions, and reviewing parts of the implementation. I did not use AI to generate the entire project or all of the code.

## What I used it for
I used AI for approximately 20% of the work, mainly for:
- Getting suggestions for the task order and implementation approach.
- Reviewing and improving some backend code and API logic.
- Getting help with parts of the EF Core setup, migrations, and API implementation.
- Getting suggestions for writing some integration tests.
- Getting occasional help with Docker configuration.
- Getting suggestions for parts of the React frontend.
- Preparing and improving the first drafts of the README, ASSUMPTIONS, and this document.

The majority of the coding, project structure, implementation, debugging, testing, and final decisions were done by me.

## Decisions I made myself
I made the main project decisions myself, including:
- One application per birth number across all classes.
- Creating a transaction number when paying the fee.
- Showing the applicant list in the admin section.
- Keeping admin and parent as separate tabs.
- Choosing the host ports.
- Deciding how the application flow and seat handling should work.

## Where AI suggestions were wrong and how I caught them
I did not blindly use the AI-generated suggestions. I ran the commands and tested the implementation myself. When something did not work, I checked the error and fixed it.

Some examples:
- **Wrong .NET version:** The initial suggestion used .NET 8, but `dotnet --version` showed 10.0.403 on my machine. I changed the project to .NET 10 and verified the configuration.
- **Port conflicts:** Some suggested ports were already being used by other containers. I checked `docker ps`, identified the conflicts, and changed the ports to 5433 and 5174.
- **Windows port issue:** `dotnet run` produced a socket permission error. I investigated the Windows excluded port range and resolved the issue.
- **Broken code:** Some AI-generated snippets caused compiler or build errors, including a stray brace, duplicated function signature, invalid JSON, and duplicated `using` statements. I identified these through the compiler, `npm run build`, and `dotnet run`, then corrected them.
- **Admin password:** An AI suggestion added a password requirement to `POST /classes`, but this did not match the project brief. I reviewed the requirement, removed the unnecessary protection, and kept protection only for the additional applicant-list endpoint.
- **Test failures:** When the tests returned `401`, I investigated the environment variables and found that `ADMIN_PASSWORD` was still set in my terminal. I corrected the test environment.

## How I checked the result
I personally tested the project rather than relying only on AI output.

I tested the endpoints manually using PowerShell and the browser, including successful requests, errors, duplicate holds, unavailable seats, confirmation, cancellation, and incorrect student information.

I also ran the integration tests. All **5 of 5 tests passed**, including the concurrency test, which sends 40 parallel requests for 5 available seats and verifies that exactly 5 holds are created while the remaining 35 requests receive `409`.

Finally, I tested the Docker setup by running `docker compose down` and `docker compose up`, checked that the data persisted, and cloned the repository fresh to verify that the project could be started without relying on my existing local setup.