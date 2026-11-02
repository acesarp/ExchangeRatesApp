# Exchange Rates App

A full-stack app that collects historical exchange rates directly from central banks around the world, caches them in SQL Server, and shows them in a React client with an interactive chart, a live Bitcoin price feed and a database health indicator.

**Live:** https://guto.ca/exchangerates

Instead of relying on a single paid FX API, the backend has a provider for each central bank (around 90 of them, from the ECB and the Bank of Canada to Banco Central do Brasil and Banxico). Each provider knows how to read that bank's data, whether it comes from a REST API, an Excel file or an HTML page.

## Features

- Historical rates for any supported currency pair over a date range
- Around 90 central bank providers, plus fixed (pegged) exchange rates and indexed units
- SQL Server cache: the API only calls a central bank for the dates it doesn't have yet
- Dates with no published rate (weekends, holidays) are recorded, so they aren't requested again
- Real-time Bitcoin price streamed from Coinbase/Kraken to the browser with SignalR
- Database health status pushed to the client every 10 seconds
- OpenID Connect sign-in and JWT-protected admin endpoints
- Structured logging with Serilog to the console, rolling files and a SQL Server `Logs` table
- Unit and integration tests with xUnit
- Containerized with Docker and deployed automatically by an Azure DevOps pipeline

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | C#, .NET 10, ASP.NET Core Web API, SignalR, background services |
| Database | SQL Server, Entity Framework Core migrations, SQL Server database project |
| Data sources | HttpClient, WebSockets, HtmlAgilityPack, ExcelDataReader, Playwright |
| Auth | OpenID Connect, JWT bearer tokens, cookie authentication |
| Logging | Serilog (console, file, MSSqlServer sink) |
| Frontend | React 19, Vite, Chart.js, SignalR client |
| Testing | xUnit, Moq, FluentAssertions, ASP.NET Core integration tests |
| DevOps | Docker, Docker Compose, nginx, Azure DevOps Pipelines (self-hosted agent), GitHub Actions |

## Architecture

```
Browser (React)
   |  HTTPS
nginx (client container)  --/exchangerates/api/-->  ASP.NET Core API (server container)
                                                     |-- QuotesController -> ExchangeRateService
                                                     |        |-- SQL Server (rate cache)
                                                     |        '-- CentralBankProviderFactory -> ECB, BoC, BCB, Banxico, ...
                                                     |-- SignalR /hubs/crypto  <- BitcoinPriceWorker <- Coinbase / Kraken
                                                     '-- SignalR /hubs/health  <- HealthStatusWorker <- SQL Server
```

How a rate request works:

1. The client asks the API for a currency pair and a date range.
2. `ExchangeRateService` loads the rates already stored in SQL Server.
3. For the missing dates, `CentralBankProviderFactory` picks the right central bank provider and fetches only what's needed.
4. New rates and unavailable dates are saved, and the full series goes back to the client.

The backend is split into four projects:

- **ExchangeRates.Server**: API, controllers, SignalR hubs, background workers and authentication
- **ExchangeRates.Domain**: entities, models and interfaces
- **ExchangeRates.Infrastructure**: EF Core `DbContext`, migrations, repository and the central bank providers
- **ExchangeRates.Server.Tests**: unit and integration tests

## Project structure

```
Clients/ExchangeRates.Client/
  exchangerates.client.react/    React + Vite client
  ExchangeRates.Blazor.Client/   Blazor WebAssembly client (earlier version)
Server/
  ExchangeRates.Server/
  ExchangeRates.Domain/
  ExchangeRates.Infrastructure/
  ExchangeRates.Server.Tests/
SQL/ExchangeRates.SQL/           SQL Server database project and data scripts
docker/
  Dockerfile.Client
  Dockerfile.Server
  docker-compose.yml
azure/main.yml                   Azure DevOps pipeline
```

## Getting started

### Prerequisites

- .NET 10 SDK
- Node.js
- SQL Server (local or in a container)
- Docker and Docker Compose

### Configuration

No secrets are stored in the repository. For Docker Compose, create a `.env` file in the `docker/` folder:

```
EXCHANGERATES_CONNECTION_STRING=Server=host.docker.internal,1433;Database=ExchangeRates;User Id=<user>;Password=<password>;TrustServerCertificate=True;
IDENTITYSERVER_AUTHORITY=<oidc-authority-url>
IDENTITYSERVER_CLIENT_ID=<client-id>
IDENTITYSERVER_CLIENT_SECRET=<client-secret>
```

When running locally, use user secrets:

```bash
cd Server/ExchangeRates.Server
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ExchangeRates" "<your connection string>"
```

Some central banks require an API key. Create a `providerkeys.json` file in the repository root (it's mounted into the server container) with a key for each provider that needs one, using the bank code:

```json
{
  "ProviderKeys": {
    "FRED": "<your-api-key>"
  }
}
```

### Database

```bash
cd Server
dotnet ef database update --project ExchangeRates.Infrastructure --startup-project ExchangeRates.Server
```

### Run with Docker Compose

```bash
docker compose -f docker/docker-compose.yml up -d --build
```

- Client: http://localhost:8082/exchangerates/
- API: http://localhost:8090/api/

### Run locally

```bash
# API (Swagger UI opens at the root in Development)
cd Server/ExchangeRates.Server
dotnet run

# React client
cd Clients/ExchangeRates.Client/exchangerates.client.react
npm install
npm run dev
```

## API

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/exchange-rates?baseCurrency=BRL&quoteCurrency=EUR&fromDate=2025-01-01&toDate=2025-10-10` | Rates for a currency pair over a date range |
| GET | `/api/available-currencies` | Supported currencies |
| GET | `/api/apphealth/status-check` | Application and database health |
| GET | `/api/environment` | Current hosting environment |
| GET | `/api/admin/login`, `/api/admin/me`, `/api/admin/logout` | Sign-in and current user (OpenID Connect) |

SignalR hubs:

| Hub | Description |
|---|---|
| `/hubs/crypto` | Live BTC-USD price |
| `/hubs/health` | Database health updates |

## Tests

```bash
cd Server
dotnet test
```

The tests cover the exchange rate service (including edge cases), rate routing and direction, the provider factory and the API controllers. Some integration tests call real central bank endpoints; `live.runsettings` and `default.runsettings` control which ones run.

## Deployment

`azure/main.yml` is an Azure DevOps pipeline that runs on a self-hosted agent on every push to `main`. It creates `providerkeys.json` from a pipeline secret, builds the images with Docker Compose, deploys the containers, checks that they're running and cleans up old images. Connection strings and identity settings come from secret pipeline variables, so nothing sensitive is stored in the repository.
