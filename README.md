# SmartCowFarm

SmartCowFarm is an Azure-native livestock monitoring solution that combines IoT telemetry, serverless processing, real-time alerts, and a web dashboard to help farmers track cow health and operations.

<img width="1862" height="897" alt="image" src="https://github.com/user-attachments/assets/34b1b72b-ae1c-496b-b15a-79c5b689b66b" />

<img width="1909" height="545" alt="image" src="https://github.com/user-attachments/assets/5be7bbd7-1223-4236-8124-e85b48b23297" />
<img width="1887" height="471" alt="image" src="https://github.com/user-attachments/assets/2146c3c4-5ca8-48a7-aeab-01e3e659db8e" />

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              Azure / Local Stack                             │
│                                                                              │
│  ┌─────────────┐   Event Hub    ┌──────────────────────────────────────────┐ │
│  │  IoT Device │ ─────────────► │         Azure Functions v4 (.NET 10)     │ │
│  │  (Telemetry)│                │  ┌────────────────────────────────────┐  │ │
│  └─────────────┘                │  │ IoTHubProcessor  (EventHub trigger) │  │ │
│                                 │  │  • Updates cow location & temp      │  │ │
│  ┌─────────────┐   HTTP/REST    │  │  • Runs alert rules engine          │  │ │
│  │  Vue 3 SPA  │ ◄────────────► │  │  • Pushes alerts via SignalR        │  │ │
│  │  (Frontend) │                │  └────────────────────────────────────┘  │ │
│  │             │   SignalR/WS   │  ┌────────────────────────────────────┐  │ │
│  │  • Dashboard│ ◄────────────  │  │ CowApi           (HTTP trigger)     │  │ │
│  │  • Map view │                │  │  • CRUD: cows, vaccinations, alerts │  │ │
│  │  • Vax table│                │  └────────────────────────────────────┘  │ │
│  └─────────────┘                │  ┌────────────────────────────────────┐  │ │
│                                 │  │ VaccinationChecker (Timer trigger)  │  │ │
│                                 │  │  • Daily at midnight (UTC)          │  │ │
│                                 │  │  • Flags vaccinations due ≤ 3 days  │  │ │
│                                 │  │  • Backfills temperature alerts     │  │ │
│                                 │  │  • Pushes alerts via SignalR        │  │ │
│                                 │  └────────────────────────────────────┘  │ │
│                                 │  ┌────────────────────────────────────┐  │ │
│                                 │  │ SignalRFunctions  (HTTP trigger)    │  │ │
│                                 │  │  • /api/negotiate  (client handshake│  │ │
│                                 │  │  • /api/broadcastUpdate             │  │ │
│                                 │  └────────────────────────────────────┘  │ │
│                                 └──────────────┬───────────────────────────┘ │
│                                                │ EF Core 9                   │
│                                 ┌──────────────▼───────────────────────────┐ │
│                                 │          SQL Server                       │ │
│                                 │  • Cows  • VaccinationRecords  • Alerts  │ │
│                                 └──────────────────────────────────────────┘ │
│                                                                              │
│                                 ┌──────────────────────────────────────────┐ │
│                                 │  Azurite (AzureWebJobsStorage emulator)  │ │
│                                 └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Backend — Azure Functions v4 (.NET 10, isolated worker)

| Function | Trigger | Responsibility |
|---|---|---|
| `CowApi` | HTTP | Full REST API: cow CRUD, vaccination records, alert listing & resolution |
| `IoTHubProcessor` | Event Hub (`telemetry`) | Deserialises telemetry payload, updates cow position/temp in DB, evaluates alert rules, broadcasts via SignalR |
| `VaccinationChecker` | Timer (`0 0 0 * * *`) | Daily scheduled scan for upcoming vaccinations (≤ 3 days) and temperature backfill; broadcasts via SignalR |
| `SignalRFunctions` | HTTP | `/api/negotiate` client handshake and `/api/broadcastUpdate` push endpoint |

#### Services

| Service | Purpose |
|---|---|
| `CowService` | Cow & vaccination CRUD, telemetry-driven field updates, query helpers for alert back-fill |
| `AlertService` | Alert persistence, unresolved alert listing, alert resolution |
| `NotificationService` | Rules engine: high/low temperature (38–39.5 °C range), geofence breach (NetTopologySuite polygon), vaccination due |

#### Data layer

- **EF Core 9** + **SQL Server 2022**
- Three tables: `Cows`, `VaccinationRecords`, `Alerts`
- `AutoCreateDatabase=true` env var calls `EnsureCreated()` on startup (dev/debug only)

#### Key NuGet packages

| Package | Use |
|---|---|
| `Microsoft.Azure.Functions.Worker` v2.1 | Isolated worker host |
| `Microsoft.Azure.Functions.Worker.Extensions.EventHubs` v6.5 | IoT/Event Hub trigger |
| `Microsoft.Azure.Functions.Worker.Extensions.SignalRService` v2.0 | Real-time push |
| `Microsoft.EntityFrameworkCore.SqlServer` v9.0 | ORM + SQL Server |
| `NetTopologySuite` v2.5 | Geofence polygon math |

---

### Frontend — Vue 3 + Vite

| Layer | Technology |
|---|---|
| Framework | Vue 3 (Composition API) |
| Build tool | Vite 8 |
| State management | Pinia |
| Routing | Vue Router 5 |
| HTTP client | Axios |
| Real-time | `@microsoft/signalr` (hub: `cowfarm`) |
| Maps | Leaflet |

#### Views & components

| Path | Purpose |
|---|---|
| `views/DashboardView.vue` | Summary cards, active alert banner |
| `views/CowListView.vue` | Paginated herd table, add/edit cow |
| `views/VaccinationView.vue` | Vaccination schedule and history |
| `components/CowMap.vue` | Leaflet map with live herd positions |
| `components/AlertBanner.vue` | Real-time alert strip (SignalR-fed) |
| `components/HealthCard.vue` | Per-cow health summary |
| `components/VaxTable.vue` | Sortable vaccination records table |
| `components/LocationPicker.vue` | Map-based lat/long picker |

The Vite dev server proxies `/api` and `/hub` to the Functions container (`http://functions:7071` in Docker, `http://localhost:7071` locally).

---

### REST API reference

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/cows` | List all cows |
| `POST` | `/api/cows` | Create a cow |
| `GET` | `/api/cows/{id}` | Get cow by ID |
| `PUT` | `/api/cows/{id}` | Update cow |
| `DELETE` | `/api/cows/{id}` | Delete cow |
| `GET` | `/api/cows/{cowId}/vaccinations` | List vaccination records |
| `POST` | `/api/cows/{cowId}/vaccinations` | Add vaccination record |
| `GET` | `/api/alerts` | List unresolved alerts |
| `PUT` | `/api/alerts/{id}/resolve` | Resolve an alert |
| `GET/POST` | `/api/negotiate` | SignalR client negotiation |
| `POST` | `/api/broadcastUpdate` | Manual SignalR broadcast |

---

## Local debugging with Docker (no local SDK/tools required)

The repository includes a full Docker debug stack. No .NET SDK, Node.js, or Azure Functions Core Tools need to be installed on the host.

### Docker containers

| Container | Image | Host port | Purpose |
|---|---|---|---|
| `smartcowfarm-functions` | Built from `Dockerfile` | `7071` | Azure Functions app (with `vsdbg` for remote attach) |
| `smartcowfarm-sql` | `mssql/server:2022-latest` | `14333→1433` | SQL Server (DB auto-created on Functions startup) |
| `smartcowfarm-azurite` | `azure-storage/azurite` | `10000–10002` | Blob/Queue/Table storage emulator |
| `smartcowfarm-frontend` | Built from `frontend/Dockerfile` | `5173` | Vite dev server with HMR |

### Relevant files

- [backend/SmartCowFarm.Functions/Dockerfile](backend/SmartCowFarm.Functions/Dockerfile)
- [docker-compose.debug.yml](docker-compose.debug.yml)
- [.vscode/tasks.json](.vscode/tasks.json)
- [.vscode/launch.json](.vscode/launch.json)

### Start and debug in VS Code

#### Option A — one-step full-stack launch
1. Open **Run and Debug** (Ctrl+Shift+D).
2. Select **Full Stack (Docker)** from the configuration dropdown.
3. Press **F5**.
   - VS Code builds and starts all four containers.
   - Attaches the .NET debugger to the `dotnet` process inside `smartcowfarm-functions`.
   - Opens the frontend at `http://localhost:5173`.
4. Set breakpoints in any backend `.cs` file and trigger endpoints from the browser.

#### Option B — manual step-by-step

**1. Start the Docker stack**
1. Open the top menu → **Terminal** → **Run Task…**
2. Select **docker: up debug stack**.
3. Wait for the task to complete (SQL Server health-check passes before Functions starts).

**2. Attach the backend debugger**
1. Open **Run and Debug** (Ctrl+Shift+D).
2. Select **Attach to Running Functions (Docker)**.
3. Press **F5**.
4. If prompted, select the `dotnet` process inside `smartcowfarm-functions`.

**3. Open the frontend**
1. Navigate to `http://localhost:5173` in a browser.
2. The Vite proxy forwards all `/api` calls to the Functions container.

### Stop the stack

Run task **docker: down debug stack**, or in a terminal:

```sh
docker compose -f docker-compose.debug.yml down -v
```

The `-v` flag removes the SQL Server data volume so the next start gets a clean database.

### Environment variables (debug stack)

| Variable | Value in debug stack |
|---|---|
| `SqlConnectionString` | Points to `smartcowfarm-sql:1433`, DB `SmartCowFarmDb` |
| `AutoCreateDatabase` | `true` — schema created by EF Core on startup |
| `AzureWebJobsStorage` | Azurite connection string |
| `AzureSignalRConnectionString` | Stub local endpoint (SignalR push disabled via `VITE_SIGNALR_ENABLED=false`) |
| `IoTHubConnectionString` | Fake local endpoint — `ProcessTelemetry` is disabled (`AzureWebJobs.ProcessTelemetry.Disabled=true`) |
| `GeofenceCoordinates` | `[[0,0],[0,1],[1,1],[1,0],[0,0]]` unit-square polygon |

### API & frontend URLs

| Service | URL |
|---|---|
| Functions REST API | `http://localhost:7071/api` |
| Frontend | `http://localhost:5173` |
| SQL Server (host) | `localhost,14333` (user `sa`, password `Your_password123`) |
| Azurite Blob | `http://localhost:10000` |

---

## Running unit tests

Tests live in `backend/SmartCowFarm.Tests` and use **xUnit**, **Moq**, and **EF Core InMemory**.

### From the terminal

```sh
cd backend
dotnet test
```

### From VS Code

1. Open the **Testing** panel (flask icon in the sidebar).
2. Click **Run All Tests** (or run individual test classes: `AlertServiceTests`, `CowApiTests`, `CowServiceTests`, `NotificationServiceTests`).

### Test project dependencies

| Package | Version |
|---|---|
| `xunit` | 2.9.2 |
| `Moq` | 4.20.72 |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.0 |
| `Microsoft.NET.Test.Sdk` | 17.11.1 |

> The test project targets **.NET 10** and references `SmartCowFarm.Functions` directly — no running containers are needed to execute tests.
