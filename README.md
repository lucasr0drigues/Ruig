# Ruig

Ruig is an API for generating a GitHub-style activity heatmap badge that can eventually combine public GitHub contribution data with Strava activity-day markers.

## Local Development

Requirements:

- .NET 10 SDK
- PostgreSQL running locally
- Strava API application

The development defaults expect PostgreSQL at:

```text
Host=localhost;Port=5432;Database=ruig;Username=postgres;Password=postgres
```

Override the connection string with user secrets when your local database differs:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=ruig;Username=postgres;Password=postgres" --project src\Ruig.Api
```

Store the Strava client secret with user secrets:

```powershell
dotnet user-secrets set "Strava:ClientSecret" "<your-strava-client-secret>" --project src\Ruig.Api
```

The development Strava redirect URI is:

```text
https://localhost:7287/auth/strava/callback
```

Use that callback URL in the Strava application settings.

If HTTPS startup fails locally, create/trust the ASP.NET development certificate:

```powershell
dotnet dev-certs https --trust
```

## OAuth Smoke Test

Apply migrations:

```powershell
dotnet ef database update --project src\Ruig.Infrastructure --startup-project src\Ruig.Api
```

Run the API:

```powershell
dotnet run --project src\Ruig.Api --launch-profile https
```

Start Strava authorization:

```text
GET https://localhost:7287/auth/strava/start
```

Open the returned `authorizationUrl` in the browser, approve the app in Strava, and confirm the callback saves an athlete and token.
