# RISK — Turn-Based Strategy Game

A browser-based RISK game with a React frontend and an ASP.NET Core API. The API handles authentication, game state, player actions, and NPC turns. It can use seeded in-memory demo data or MongoDB for persistent users and games.

## Requirements

- .NET 10 SDK
- Node.js and npm (use a current Node.js LTS release)
- MongoDB Atlas only if you want persistent database storage; demo mode does not require MongoDB

## Run the demo locally

Demo mode is enabled by default for the included Development launch profiles. It uses seeded accounts and stores accounts and games in memory, so that data resets when the API stops.

1. Open a terminal in the repository root.
2. Start the API:

   ```powershell
   cd UCTWebGame
   dotnet restore
   dotnet run --launch-profile http
   ```

   The API listens at `http://localhost:5138`.

3. Open a second terminal in the repository root and start the frontend:

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

   Open the Vite URL printed in the terminal (normally `http://localhost:51493`). The frontend uses `http://localhost:5138/api` by default.

4. Sign in with one of the demo accounts:

   | Account | Email | Password |
   | --- | --- | --- |
   | Commander | `commander@frontline.demo` | `FrontlineDemo1!` |
   | Scout | `scout@frontline.demo` | `FrontlineDemo2!` |

   The sign-in screen provides demo-account buttons while running through Vite in development mode.

## Use MongoDB Atlas for persistent data (optional)

The API uses demo mode by default in Development. To use MongoDB instead, configure the API's .NET User Secrets from the repository root. Keep the Atlas connection string and JWT secret private; do not commit them or put them in frontend environment variables.

The project already has a `UserSecretsId`. Replace the example values below with your Atlas connection string, the database name you want to use, and a private JWT secret with at least 32 UTF-8 bytes:

```powershell
dotnet user-secrets set --project .\UCTWebGame\UCTWebGame.csproj "MongoDb:ConnectionString" "mongodb+srv://<db-user>:<db-password>@<cluster-host>/?retryWrites=true&w=majority"
dotnet user-secrets set --project .\UCTWebGame\UCTWebGame.csproj "MongoDb:DatabaseName" "UCTWebGame"
dotnet user-secrets set --project .\UCTWebGame\UCTWebGame.csproj "Jwt:Secret" "<private-secret-at-least-32-characters>"
dotnet user-secrets set --project .\UCTWebGame\UCTWebGame.csproj "DemoMode:Enabled" "false"
dotnet user-secrets set --project .\UCTWebGame\UCTWebGame.csproj "Cors:AllowedOrigins:0" "http://localhost:51493"
```

Also add the API machine's public IP address to the Atlas project's Network Access IP Access List, and ensure the Atlas database user has the required database permissions. Restart the API after changing these settings:

```powershell
cd .\UCTWebGame
dotnet run --launch-profile http
```

Then run the frontend as shown above. New registrations and games will be stored in the configured MongoDB database. The first API start in MongoDB mode creates the required indexes.

> The MongoDB URI and JWT secret are examples only. Do not paste real credentials into this README, source code, or GitHub.

## Configuration

- `MongoDb:ConnectionString` — MongoDB connection string; use User Secrets locally.
- `MongoDb:DatabaseName` — database name used by the API.
- `Jwt:Secret` — private signing key, at least 32 UTF-8 bytes.
- `DemoMode:Enabled` — set to `false` to use MongoDB in Development. Demo mode is only enabled in the Development environment.
- `Cors:AllowedOrigins:0` — allowed frontend origin when demo mode is disabled. The Vite config uses port `51493`.
- `VITE_API_BASE_URL` — optional frontend API base URL; defaults to `http://localhost:5138/api`.
- `Gemini:ApiKey` — optional API key for Gemini-powered commentary. The game has fallback commentary if no key is configured.

## Useful commands

Run the API from the repository root:

```powershell
dotnet run --project .\UCTWebGame\UCTWebGame.csproj --launch-profile http
```

Build the frontend:

```powershell
cd frontend
npm run build
```

Run the frontend linter:

```powershell
cd frontend
npm run lint
```
