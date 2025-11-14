# Hello Aspire Example

## What This Example Shows

The simplest possible .NET Aspire application - just an AppHost that displays a welcome message in the dashboard.

## Running the Example

```bash
dotnet run
```

The Aspire Dashboard will open at `http://localhost:15888` showing the running resources.

## What You'll See

- The Aspire Dashboard opens automatically
- The "Resources" tab shows your application resources
- Even though there are no services yet, you'll see the orchestration working

## Code Structure

```
01-hello-aspire/
└── HelloAspire.AppHost/
    ├── Program.cs          # Main AppHost file
    ├── appsettings.json    # Configuration
    └── HelloAspire.AppHost.csproj
```

## Key Concepts Demonstrated

1. **DistributedApplicationBuilder** - Creates the application
2. **Build and Run** - Starts the Aspire orchestration
3. **Dashboard** - Automatic observability UI

## Next Steps

- Add a web service: [Multi-Service App](../02-multi-service/)
- Learn more: [AppHost Fundamentals](../../topics/02-apphost.md)

## Try It

Modify `Program.cs` to experiment:
- Change the dashboard port
- Add logging statements
- Explore the builder API
