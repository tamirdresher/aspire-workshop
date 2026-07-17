# Lesson 1: Getting Started with Aspire

## Introduction

In this lesson, you'll learn how to add Aspire to an existing Bookstore application. Aspire provides powerful capabilities for building cloud-native, distributed applications including:

- **Service Defaults**: Smart defaults for telemetry, resiliency, health checks, and service discovery
- **Orchestration**: App Host project to manage and run multiple services together
- **Dashboard**: Built-in developer dashboard for monitoring logs, traces, metrics, and more
- **Service Discovery**: Automatic service-to-service communication without hardcoded URLs
- **Integrations**: Easy integration with databases, caching, messaging, and other services

By the end of this lesson, you'll have transformed a plain .NET solution into an Aspire-powered application with a Bookstore API, a Blazor Web frontend, and an Admin application.

## Prerequisites

- .NET 10 SDK installed
- Aspire CLI 13.4.6 ([installation guide](https://aspire.dev/get-started/install-cli/))
- Visual Studio 2026 or Visual Studio Code with C# Dev Kit
- Node.js 20.19+ and npm
- Basic understanding of ASP.NET Core and Blazor

Verify the CLI and local prerequisites before starting:

```bash
aspire --version
aspire doctor
```

Unless a step says otherwise, run command-line examples from the repository
root. This lesson creates AppHost and Service Defaults as separate projects, so
install the matching granular templates once:

```bash
dotnet new install Aspire.ProjectTemplates::13.4.6
```

## Starting Point

The `/start` folder contains a basic Bookstore application with:
- [`Bookstore.API`](../../start/Bookstore.API/Program.cs) - A Minimal API serving book data
- [`Bookstore.Web`](../../start/Bookstore.Web/Bookstore.Web/Program.cs) - A Blazor Web App displaying books
- [`Bookstore.Shared`](../../start/Bookstore.Shared/Models.cs) - Shared models (Book, Order)

Currently, the Web app connects to the API using a hardcoded URL (`https://localhost:7032`). We'll improve this with Aspire!

## Choose Your AppHost Track

The application services remain in .NET. For orchestration, choose one AppHost language and use it throughout Lessons 1 and 2:

| Track | AppHost | Best for |
| --- | --- | --- |
| C# | [`code/Bookstore.AppHost/Program.cs`](./code/Bookstore.AppHost/Program.cs) | The original workshop flow and Visual Studio AppHost tooling |
| TypeScript | [`code/Bookstore.TypeScriptAppHost/apphost.mts`](./code/Bookstore.TypeScriptAppHost/apphost.mts) | The Aspire GA polyglot AppHost model using `apphost.mts` |

Both AppHosts orchestrate the same API, Web, Worker, and Vite Admin projects. Prepare the shared application from the lesson's `code` directory:

```bash
cd Exercise/workshop/Lesson-01/code
dotnet restore Bookstore.sln
npm --prefix Bookstore.Admin ci
```

Start the **C# AppHost**:

```bash
aspire run --apphost Bookstore.AppHost/Bookstore.AppHost.csproj
```

Or prepare and start the **TypeScript AppHost**:

```bash
npm --prefix Bookstore.TypeScriptAppHost ci
aspire restore --apphost Bookstore.TypeScriptAppHost
npm --prefix Bookstore.TypeScriptAppHost run build
npm --prefix Bookstore.TypeScriptAppHost run dev
```

The `dev` script performs a Release solution build before the TypeScript AppHost starts the shared .NET projects together, preventing concurrent first-build output races on Windows. The TypeScript track pins the Aspire SDK and JavaScript hosting integration to `13.4.6`. `aspire restore` generates the local `.aspire/modules` API; do not edit those generated files. Press `Ctrl+C` to stop the selected AppHost.

---

## Part A: Add Service Defaults Project

Service Defaults provide a centralized place to configure common cross-cutting concerns like telemetry, health checks, and resiliency for all services in your application.

### What are Service Defaults?

Aspire's Service Defaults automatically configure:
- **Telemetry**: OpenTelemetry for metrics, tracing, and logging
- **Resiliency**: Polly policies for HTTP retries and circuit breakers
- **Health Checks**: Endpoints for monitoring service health (`/health`, `/alive`)
- **Service Discovery**: Configuration-based endpoint resolution

### Create the ServiceDefaults Project

#### Visual Studio & Visual Studio Code

1. Add a new project to the solution called [`Bookstore.ServiceDefaults`](./code/Bookstore.ServiceDefaults/Bookstore.ServiceDefaults.csproj):
   - Right-click on the solution and select `Add` > `New Project`
   - Select the **Aspire Service Defaults** project template
   - Name the project `Bookstore.ServiceDefaults`
   - Click `Next` > `Create`

![Aspire Service Defaults](../media/vs-add-servicedefaults.png)

In VS Code it looks like this:

![VS Code Service Defaults template](../media/vscode-add-servicedefaults.png)

#### Command Line

1. Create a new project using the `dotnet new aspire-servicedefaults` command:

```bash
dotnet new aspire-servicedefaults -n Bookstore.ServiceDefaults -o Exercise/start/Bookstore.ServiceDefaults
```

2. Add the new ServiceDefaults project to your solution:

```bash
dotnet sln Exercise/start/Bookstore.sln add Exercise/start/Bookstore.ServiceDefaults/Bookstore.ServiceDefaults.csproj
```

### Configure Projects to Use Service Defaults

Now we need to add references to the ServiceDefaults project and call its extension methods in both the API and Web projects.

**Why these steps?**
- Adding project references allows the API and Web projects to consume the shared configuration
- Calling `builder.AddServiceDefaults()` applies the opinionated smart defaults (telemetry, health checks, service discovery, resilient HTTP)
- Calling `app.MapDefaultEndpoints()` maps health endpoints (`/health`, `/alive`) for diagnostics and readiness probes

#### 1. Add ServiceDefaults Reference to API Project

**Visual Studio/VS Code**: Right-click on the [`Bookstore.API`](../../start/Bookstore.API/Bookstore.API.csproj) project → `Add` > `Reference` → Check `Bookstore.ServiceDefaults` → Click `OK`

**Command Line**:
```bash
dotnet add Exercise/start/Bookstore.API/Bookstore.API.csproj reference Exercise/start/Bookstore.ServiceDefaults/Bookstore.ServiceDefaults.csproj
```

#### 2. Update API Program.cs

Open [`Bookstore.API/Program.cs`](../../start/Bookstore.API/Program.cs) and add the following:

Add `builder.AddServiceDefaults();` immediately after `var builder = WebApplication.CreateBuilder(args);`:

```csharp
using Bookstore.Shared;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();  // Add this line

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();  // Add this line

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Rest of the code...
```

#### 3. Add ServiceDefaults Reference to Web Project

**Visual Studio/VS Code**: Right-click on the [`Bookstore.Web`](../../start/Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj) project → `Add` > `Reference` → Check `Bookstore.ServiceDefaults` → Click `OK`

**Command Line**:
```bash
dotnet add Exercise/start/Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj reference Exercise/start/Bookstore.ServiceDefaults/Bookstore.ServiceDefaults.csproj
```

#### 4. Update Web Program.cs

Open [`Bookstore.Web/Bookstore.Web/Program.cs`](../../start/Bookstore.Web/Bookstore.Web/Program.cs) and add the following:

Add `builder.AddServiceDefaults();` immediately after `var builder = WebApplication.CreateBuilder(args);`:

```csharp
using Bookstore.Web.Client.Pages;
using Bookstore.Web.Components;
using Bookstore.Web.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();  // Add this line

builder.Services.AddHttpClient<BookstoreClient>(client =>
{
    client.BaseAddress = new("https://localhost:7032");
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

app.MapDefaultEndpoints();  // Add this line

// Rest of the code...
```

### Verify Service Defaults

Build the solution to ensure everything compiles:

```bash
dotnet build Exercise/start/Bookstore.sln
```

You can now run both projects and test the health endpoints:
- API: `https://localhost:7032/health`
- Web: `https://localhost:7265/health`

You should see output like `Healthy` indicating the health checks are working!

---

## Part B: Add App Host Project

The App Host project orchestrates your services, making it easy to run multiple projects together and providing the Aspire Dashboard for monitoring.

The creation steps below describe the C# track. TypeScript-track users can use the checked-in [`Bookstore.TypeScriptAppHost`](./code/Bookstore.TypeScriptAppHost/) and compare the complete `apphost.mts` example later in this lesson.

### What is the App Host?

The App Host (also called Orchestrator) is a .NET project that:
- Defines your application model (which services, containers, and resources exist)
- Starts and manages all services together
- Provides the Aspire Dashboard at development time
- Configures service-to-service communication

### Create the AppHost Project

#### Visual Studio & Visual Studio Code

1. Add a new project to the solution called [`Bookstore.AppHost`](./code/Bookstore.AppHost/Bookstore.AppHost.csproj):
   - Right-click on the solution and select `Add` > `New Project`
   - Select the **Aspire App Host** project template
   - Name the project `Bookstore.AppHost`
   - Click `Next` > `Create`

![Aspire App Host](../media/vs-add-apphost.png)

#### Command Line

1. Create a new project using the `dotnet new aspire-apphost` command:

```bash
dotnet new aspire-apphost -n Bookstore.AppHost -o Exercise/start/Bookstore.AppHost
```

2. Add the new AppHost project to your solution:

```bash
dotnet sln Exercise/start/Bookstore.sln add Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj
```

### Add Project References

The AppHost needs references to the projects it will orchestrate.

#### Visual Studio/VS Code

Right-click on the [`Bookstore.AppHost`](./code/Bookstore.AppHost/Bookstore.AppHost.csproj) project → `Add` > `Reference` → Check both `Bookstore.API`, `Bookstore.Web` and `Bookstore.Worker` → Click `OK`

> **Pro Tip**: In Visual Studio, you can drag and drop projects onto the AppHost project to add references.

#### Command Line

```bash
dotnet add Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj reference Exercise/start/Bookstore.API/Bookstore.API.csproj
dotnet add Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj reference Exercise/start/Bookstore.Worker/Bookstore.Worker.csproj
dotnet add Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj reference Exercise/start/Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj
```

When these references are added, helper classes are automatically generated to help add them to the app model.

### Orchestrate the Application

Open [`Bookstore.AppHost/Program.cs`](./code/Bookstore.AppHost/Program.cs) or (AppHost.cs) and add your projects to the app model:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Bookstore_API>("api");

var web = builder.AddProject<Projects.Bookstore_Web>("web");

var worker = builder.AddProject<Projects.Bookstore_Worker>("worker");

builder.Build().Run();
```

> **Note**: The project names use underscores (`Bookstore_API`, `Bookstore_Web`) instead of dots because they're generated as C# identifiers.

### Run the Application

#### Set AppHost as Startup Project

**Visual Studio**: Right-click the [`Bookstore.AppHost`](./code/Bookstore.AppHost/Bookstore.AppHost.csproj) project → `Set as Startup Project`

**Visual Studio Code**: Create or update `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Run AppHost",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj"
    }
  ]
}
```

#### Launch the Dashboard

Press `F5` or click `Start Debugging`. To use the CLI from the repository root,
run the AppHost explicitly:

```bash
aspire run --apphost Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj
```

The CLI prints the authenticated **Aspire Dashboard** URL after the AppHost
starts. Open that URL instead of assuming a fixed local port.

To run either checked-in completed AppHost instead, use the commands in
[Choose Your AppHost Track](#choose-your-apphost-track).

> **Using an AI coding agent?** Use `aspire start` for background execution, add
> `--isolated` in a worktree, and call `aspire wait` before interacting with a resource.
> See [AI coding agents and Aspire skills](../../../docs/ai-agents-and-aspire-skills.md).

![Aspire Dashboard](../media/dashboard.png)

The dashboard shows:
- **Resources**: All running services (api, web)
- **Console Logs**: Real-time logs from each service
- **Traces**: Distributed tracing across services
- **Metrics**: Performance metrics
- **Structured Logs**: Filterable log entries

#### Explore the Dashboard

1. **View Endpoints**: Click the endpoint shown for the `web` project to open the Bookstore website
2. **View Logs**: Click `View Logs` for any resource to see console output
3. **View Traces**: Navigate to the `Traces` tab, then click `View` on a trace to see the request flow
4. **View Metrics**: Explore the `Metrics` tab to see HTTP request duration, request rates, and more

---

## Part C: Configure Service Discovery

Currently, the Web app still uses a hardcoded URL to connect to the API. Let's use Aspire's service discovery to make this dynamic!

### What is Service Discovery?

Service discovery allows services to reference each other by name (e.g., `api`) instead of hardcoded URLs. Aspire automatically:
- Resolves service names to actual endpoints at runtime
- Handles multiple endpoints (http/https)
- Works in development and production environments
- Updates configuration automatically when services move

### Update AppHost to Enable Service Discovery

Open [`Bookstore.AppHost/Program.cs`](./code/Bookstore.AppHost/Program.cs) and update it to add a reference from web to api:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Bookstore_API>("api");

var web = builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)                  // Enable service discovery
    .WithExternalHttpEndpoints();        // Allow external access

builder.AddProject<Projects.Bookstore_Worker>("worker")
    .WithReference(api);

builder.Build().Run();
```

**What does this do?**
- `WithReference(api)`: Injects configuration into the Web project so it can discover the API by name
- `WithExternalHttpEndpoints()`: Makes the web service accessible from outside (needed for deployment)

### Update Web App to Use Service Discovery

Service discovery in Aspire works through configuration. The AppHost injects settings like `services__api__http__0` and `services__api__https__0` into the Web project.

#### Option 1: Update the HttpClient BaseAddress

Open [`Bookstore.Web/Bookstore.Web/Program.cs`](../../start/Bookstore.Web/Bookstore.Web/Program.cs) and change the hardcoded URL to use service discovery:

```csharp
builder.Services.AddHttpClient<BookstoreClient>(client =>
{
    client.BaseAddress = new("https+http://api");  // Changed from https://localhost:7032
});
```

**About `https+http://api`:**
- The scheme `https+http` tells the resolver to prefer HTTPS if available, otherwise fall back to HTTP
- `api` is the name we gave the API project in the AppHost
- Multiple schemes are evaluated left-to-right, separated by `+`
- This works for local development (HTTP only) and production (HTTPS) without changes

> **Important**: Only use multi-scheme URIs for internal service-to-service communication. Don't expose them in user-facing URLs.

#### Option 2: Use Configuration (Alternative)

Alternatively, you can use the existing configuration approach. Create or update [`appsettings.json`](../../start/Bookstore.Web/Bookstore.Web/appsettings.json):

```json
{
  "BookstoreApiUrl": "https+http://api"
}
```

Then update [`Program.cs`](../../start/Bookstore.Web/Bookstore.Web/Program.cs):

```csharp
builder.Services.AddHttpClient<BookstoreClient>(client =>
{
    var apiUrl = builder.Configuration["BookstoreApiUrl"] ?? "https://localhost:7032";
    client.BaseAddress = new(apiUrl);
});
```

Do the same for the worker project

### Test Service Discovery

1. Run the AppHost (`F5`)
2. Open the Aspire Dashboard
3. Click on the `web` project's Details
4. Click the eye icon to reveal configuration values
5. Scroll to see `services__api__http__0` and `services__api__https__0` with the API's actual URLs

![Service Discovery in Dashboard](../media/dashboard-servicediscovery.png)

6. Open the web endpoint and verify the bookstore still works!

The Web app is now discovering the API automatically. If you change the API's port, it will still work without code changes!

---

## Part D: Adding the Admin React Application

Now let's add a JavaScript-based React application to our Aspire orchestration. The Admin app provides a web interface for managing books and viewing orders.

### What is the Admin App?

The Admin app is a React application built with Vite that allows administrators to:
- View and manage the book inventory
- Add new books to the catalog
- Delete books from the inventory
- View customer orders

### Why AddViteApp?

Aspire provides `AddViteApp()` specifically for Vite-based applications. This method:
- Runs the Vite development script with an Aspire-assigned port
- Automatically configures environment variables for service discovery
- Handles lifecycle management (start/stop)
- Integrates with the Aspire Dashboard for monitoring
- Supports both the C# and TypeScript AppHost APIs

### Prerequisites

Before adding the Admin app, ensure you have:
- **Node.js** installed (v20.19 or higher)
- **npm** package manager
- The [`Bookstore.Admin`](../../start/Bookstore.Admin) folder in your project

### Step 1: Add the Aspire JavaScript Hosting Package

The AppHost needs a NuGet package to support JavaScript applications.

**Command Line (recommended)**:
```bash
aspire integration search javascript
aspire add javascript --apphost Exercise/start/Bookstore.AppHost/Bookstore.AppHost.csproj
```

`aspire integration search` is read-only and confirms the current official
integration before `aspire add` selects the integration version for the
AppHost's configured channel. On the stable 13.4.6 SDK used by this workshop,
`aspire add` adds `Aspire.Hosting.JavaScript` 13.4.6.

**Visual Studio/VS Code**:
- Right-click on [`Bookstore.AppHost`](./code/Bookstore.AppHost/Bookstore.AppHost.csproj) project → `Manage NuGet Packages`
- Search for `Aspire.Hosting.JavaScript`
- Install version `13.4.6` to match the AppHost SDK and align with the repository's
  central package management

### Step 2: Update AppHost to Add the Admin App

Open [`Bookstore.AppHost/Program.cs`](./code/Bookstore.AppHost/Program.cs) and add the Admin app:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Bookstore_API>("api");

var web = builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Add Admin React app
builder.AddViteApp("admin", "../Bookstore.Admin")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Understanding Each Method

Let's break down what each method does:

#### `AddViteApp("admin", "../Bookstore.Admin")`
- Registers a Vite application with Aspire
- `"admin"` is the resource name shown in the dashboard
- `"../Bookstore.Admin"` is the relative path to the Node.js project directory
- Aspire runs `npm run dev` and supplies the endpoint port

#### `WithReference(api)`
- Enables service discovery from the Admin app to the API
- Injects environment variables like `services__api__http__0` and `services__api__https__0`
- The Admin app can use these to dynamically discover the API URL
- No hardcoded URLs needed!

#### `WithExternalHttpEndpoints()`
- Makes the Admin app accessible from outside the local machine
- Required for deployment scenarios (Azure Container Apps, Kubernetes, etc.)
- Without this, the endpoint would only be accessible within the Aspire network

### Step 3: Configure the Admin App to Use Service Discovery

The checked-in Admin app sends requests to `/api`. Its Vite development proxy reads the `API_HTTP` endpoint injected by `.WithReference(api)`, so browser requests reach the Aspire-managed API without a hardcoded port.

Open `Bookstore.Admin/src/App.jsx` in your working copy and update line 4:

**Before:**
```javascript
const API_BASE_URL = 'https://localhost:7032'
```

**After:**
```javascript
const API_BASE_URL = '/api'
```

Configure `vite.config.js` to proxy that path:

```javascript
server: {
  host: true,
  proxy: {
    '/api': {
      target: process.env.API_HTTP || 'https://localhost:7032',
      changeOrigin: true,
      secure: false,
      rewrite: (path) => path.replace(/^\/api/, ''),
    },
  },
}
```

### Step 4: Run and Verify

Now let's test the Admin app integration!

1. **Start the AppHost** using the command for your selected track in [Choose Your AppHost Track](#choose-your-apphost-track), or press `F5` for the C# project.

2. **Open the Aspire Dashboard** - it opens automatically when the AppHost starts.
   The port is assigned dynamically, so open the dashboard URL printed in the CLI
   output (don't assume a fixed `localhost` port).

3. **Verify Admin appears** in the Resources tab:
   - You should see a resource named `admin`
   - Status should show `Running`
   - The runtime-assigned endpoint URL will be displayed

4. **View Admin Logs**:
   - Click `View Logs` for the `admin` resource
   - You should see Vite's development server output
   - Confirm that Vite reports the same endpoint shown by the dashboard

5. **Access the Admin UI**:
   - Click the endpoint link for the `admin` resource
   - The Admin UI should load in your browser
   - You should see the "Bookstore Admin Panel" with tabs for Books, Add Book, and Orders

6. **Test API Communication**:
   - Click the "Books" tab
   - The Admin app should fetch and display books from the API
   - If you see books listed, service discovery is working! 🎉

> **💡 CLI tip:** You don't have to leave the terminal to inspect the app. With the
> AppHost running, try `aspire describe` to see every resource, its state, and its
> endpoints; `aspire logs admin --follow` to tail console output;
> `aspire otel logs api --search "severity:error"` to search structured logs; and
> `aspire otel traces --search "@http.status_code:500"` to find failed request
> traces. Search runs server-side before results are returned. See the
> [CLI, Dashboard & Observability guide](../../../docs/cli-dashboard-observability.md).

### Complete AppHost Example

Here's what your complete [`Program.cs`](./code/Bookstore.AppHost/Program.cs) should look like after adding all parts (A through D):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Part A & B: Add API project with service defaults
var api = builder.AddProject<Projects.Bookstore_API>("api");

// Part C: Add Web project with service discovery
var web = builder.AddProject<Projects.Bookstore_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Part D: Add Admin React app
builder.AddViteApp("admin", "../Bookstore.Admin")
    .WithReference(api)
    .WithExternalHttpEndpoints();

// Add Worker service
builder.AddProject<Projects.Bookstore_Worker>("worker")
    .WithReference(api);

builder.Build().Run();
```

The equivalent GA TypeScript AppHost is [`Bookstore.TypeScriptAppHost/apphost.mts`](./code/Bookstore.TypeScriptAppHost/apphost.mts):

```typescript
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const api = builder.addProject('api', '../Bookstore.API/Bookstore.API.csproj');

await builder
  .addProject('web', '../Bookstore.Web/Bookstore.Web/Bookstore.Web.csproj')
  .withReference(api)
  .waitFor(api)
  .withExternalHttpEndpoints();

await builder
  .addProject('worker', '../Bookstore.Worker/Bookstore.Worker.csproj')
  .withReference(api)
  .waitFor(api);

await builder
  .addViteApp('admin', '../Bookstore.Admin')
  .withReference(api)
  .waitFor(api)
  .withExternalHttpEndpoints();

await builder.build().run();
```

### Benefits of Orchestrating JavaScript Apps with Aspire

#### 1. **Unified Developer Experience**
- Manage all services (.NET and JavaScript) from a single dashboard
- Start/stop everything with one command
- Consistent monitoring and logging across all technologies

#### 2. **Service Discovery**
- JavaScript apps can discover .NET services by name
- No environment-specific configuration files
- Works the same in development, staging, and production

#### 3. **Automatic Configuration**
- Aspire injects connection strings and service URLs automatically
- No manual configuration needed
- Environment variables are managed for you

#### 4. **Framework-Aware Hosting**
- `AddViteApp()` configures the Vite development server and endpoint
- The same application model is available from C# and TypeScript AppHosts
- Publish behavior can be added explicitly for the deployment target

#### 5. **Observability**
- All JavaScript app logs appear in the Aspire Dashboard
- Monitor performance and errors alongside .NET services
- Integrated tracing across service boundaries

### How Service Discovery Works in JavaScript Apps

When you use `WithReference(api)` in the AppHost, Aspire:

1. **Injects environment variables** into the Admin app:
   ```
   API_HTTP=http://localhost:<assigned-port>
   ```

2. **The Vite development proxy reads the endpoint** in `vite.config.js`:
   ```javascript
   target: process.env.API_HTTP
   ```

3. **Browser requests use the same-origin proxy path**:
   ```javascript
   fetch('/api/books')
   ```

4. **Aspire updates the URLs automatically** when services move or ports change

This approach works with any JavaScript framework (React, Vue, Angular, Next.js, Express, etc.)!

### Why the Vite-Specific API?

`AddViteApp()` understands Vite's development command and endpoint behavior. Use the generic `AddJavaScriptApp()` or `AddNodeApp()` for JavaScript applications that are not Vite frontends.

### Troubleshooting

**Admin app not starting?**
- Ensure `npm install` was run in the `Bookstore.Admin` directory
- Check the Admin logs in the Aspire Dashboard for errors
- Verify Node.js is installed: `node --version`

**Can't connect to API?**
- Check that the API resource is running in the dashboard
- Verify the service discovery environment variables are injected (click Details on the admin resource)
- Verify that `API_HTTP` is present and that the Vite proxy targets it

**Port conflicts?**
- Aspire automatically assigns dynamic ports
- If you have a conflict, Aspire will choose a different port
- Check the actual endpoint in the dashboard

---

## Understanding the Dashboard

The Aspire Dashboard is a powerful tool for local development. Let's explore its features:

AppHost dashboard, resource-service, and OTLP endpoints are runtime configuration.
Aspire 13.4 dynamically assigns the supporting ports, so use the authenticated
dashboard URL printed at startup and the OTLP settings injected into resources.
Do not copy local port numbers into tests or workshop instructions.

### Resources Tab

Shows all services, containers, and projects:
- **Status**: Running, Stopped, or Failed
- **Endpoints**: Click to open in browser
- **Actions**: View Logs, Details, Restart, Stop

### Console Logs

Real-time streaming logs from all resources. Features:
- Filter by resource
- Search within logs
- Text wrapping toggle

### Structured Logs

Filterable, queryable logs with:
- Log level filtering (Error, Warning, Info, Debug)
- Time range selection
- Full-text search
- Trace ID linking

### Traces

Distributed tracing shows:
- Request flow across services
- Timing breakdown
- Span details
- Error detection

The CLI provides the same server-side search workflow for terminal-based
investigation:

```bash
aspire logs api --search "timeout"
aspire otel logs api --search "severity:error"
aspire otel traces --search "status:error duration:>500"
```

![Dashboard Trace View](../media/dashboard-trace.png)

### Metrics

Performance metrics include:
- HTTP request duration
- Request rates
- Memory usage
- CPU usage

![Dashboard Metrics](../media/dashboard-metrics.png)

---

## Summary

In this lesson, you've learned how to:

✅ Add Service Defaults to configure telemetry, health checks, and resiliency
✅ Create an App Host to orchestrate multiple services
✅ Use the Aspire Dashboard for monitoring and debugging
✅ Implement service discovery for dynamic service-to-service communication
✅ Orchestrate JavaScript/Node.js applications alongside .NET services
✅ Enable service discovery for React apps to communicate with .NET APIs

Your Bookstore application is now powered by Aspire with improved observability, resiliency, and developer experience! You've successfully integrated both .NET and JavaScript applications in a unified orchestration system.

## Next Steps

In [Lesson 2](../Lesson-02/README.md), you'll learn how to:
- Add Redis caching for improved performance
- Integrate Azure Cosmos DB (starting with emulator, then migrating to cloud)
- Implement comprehensive health checks
- Explore telemetry and deployment to Azure

---

## Learn More

- [Aspire documentation](https://aspire.dev/)
- [Service defaults](https://aspire.dev/get-started/csharp-service-defaults/)
- [AppHost overview](https://aspire.dev/get-started/app-host/)
- [Service discovery](https://aspire.dev/fundamentals/service-discovery/)
- [Dashboard overview](https://aspire.dev/dashboard/overview/)
- [CLI, Dashboard & Observability guide](../../../docs/cli-dashboard-observability.md)
- [AI coding agents and Aspire skills](../../../docs/ai-agents-and-aspire-skills.md)
