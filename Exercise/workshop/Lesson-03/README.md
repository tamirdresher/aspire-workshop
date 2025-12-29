# Lesson 3: Building Custom Resources and Integration Testing

In this lesson, we will dive deep into the **Aspire Resource Model** by building a completely custom resource from scratch. We will also learn how to write integration tests for our Aspire application.

## Goals

1.  **Master the Aspire Resource Model**: Understand how resources, annotations, and lifecycle events work together.
2.  **Build a "Talking Clock" Resource**: Create a custom resource that simulates a clock, updates its state in real-time, and manages child resources.
3.  **Implement Integration Tests**: Use `Aspire.Hosting.Testing` to verify your application's behavior.

---

## Part 1: Building a Custom Resource (The Talking Clock)

To understand how Aspire works under the hood, we are going to build a **Talking Clock**.

### What is the Talking Clock?

The Talking Clock is a custom resource that:
*   **Ticks and Tocks**: It simulates a clock mechanism.
*   **Has "Hands"**: It manages two child resources, `TickHand` and `TockHand`, demonstrating parent-child relationships.
*   **Updates State**: It pushes real-time state updates to the Aspire Dashboard (e.g., "Tick", "Tock", "On", "Off").
*   **Logs Time**: It writes the current time to the console logs.

This exercise will teach you how to define resources, manage their lifecycle, and interact with the Aspire dashboard programmatically.

### Step 1: Define the Resource Class

First, we need a class to represent our resource in the application model. All resources in Aspire implement `IResource`, and typically inherit from the `Resource` base class.

Create a new file `TalkingClockResource.cs` in `Bookstore.AppHost`:

```csharp
using Aspire.Hosting;

namespace Bookstore.AppHost;

// The main resource class representing the clock
public sealed class TalkingClockResource(string name, ClockHandResource tickHand, ClockHandResource tockHand) : Resource(name)
{
    // Child resources managed by this clock
    public ClockHandResource TickHand { get; } = tickHand;
    public ClockHandResource TockHand { get; } = tockHand;
}

// A simple resource representing a clock hand
public sealed class ClockHandResource(string name) : Resource(name);
```

**Key Takeaways:**
*   **`Resource(name)`**: The base class constructor sets the unique name of the resource.
*   **Composition**: We are composing our resource with other resources (`ClockHandResource`), which allows us to model complex systems.

### Step 2: Create the Builder Extension

In Aspire, resources are added to the application using **Fluent Extension Methods**. This pattern encapsulates the construction and configuration of the resource.

Create a new file `TalkingClockResourceBuilderExtensions.cs` in `Bookstore.AppHost`. We will build this up step-by-step.

#### 2.1. The `AddTalkingClock` Method

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Bookstore.AppHost;

public static class TalkingClockResourceBuilderExtensions
{
    public static IResourceBuilder<TalkingClockResource> AddTalkingClock(this IDistributedApplicationBuilder builder, string name)
    {
        // 1. Create the resource instances
        var tickHandResource = new ClockHandResource(name + "-tick-hand");
        var tockHandResource = new ClockHandResource(name + "-tock-hand");
        var resource = new TalkingClockResource(name, tickHandResource, tockHandResource);

        // 2. Register the main resource with the builder
        var clockBuilder = builder.AddResource(resource)
                      .WithInitialState(new()
                      {
                          ResourceType = "TalkingClock",
                          CreationTimeStamp = DateTime.UtcNow,
                          State = KnownResourceStates.NotStarted,
                          Properties = [
                              new(CustomResourceKnownProperties.Source, "Talking Clock")
                          ]
                      })
                      .WithUrl("https://www.speaking-clock.com/", "Speaking Clock")
                      .ExcludeFromManifest(); // Custom resources often don't need to be in the deployment manifest

        // ... Lifecycle logic will go here ...

        // 3. Register child resources
        AddHandResource(tickHandResource);
        AddHandResource(tockHandResource);

        return clockBuilder;

        // Helper to add child resources
        void AddHandResource(ClockHandResource clockHand)
        {
            builder.AddResource(clockHand)
                .WithParentRelationship(clockBuilder) // Visual nesting in Dashboard
                .WithInitialState(new()
                {
                    ResourceType = "ClockHand",
                    CreationTimeStamp = DateTime.UtcNow,
                    State = KnownResourceStates.NotStarted,
                    Properties = [ new(CustomResourceKnownProperties.Source, "Talking Clock") ]
                });
        }
    }
}
```

**Key Takeaways:**
*   **`AddResource`**: Registers the resource with the dependency injection container and the app model.
*   **`WithInitialState`**: Provides metadata for the dashboard before the resource even starts.
*   **`ExcludeFromManifest`**: Since this is a logical resource for simulation and not a container/executable we deploy, we exclude it from the manifest.
*   **`WithParentRelationship`**: Tells the dashboard to render the hands nested under the clock.

### Step 3: Implement Lifecycle Logic

Now, we need to make the clock actually *do* something. We use the `OnInitializeResource` hook to define behavior that runs when the resource starts.

Add this code inside the `AddTalkingClock` method (where the comment `// ... Lifecycle logic will go here ...` is):

```csharp
        clockBuilder.OnInitializeResource(static async (resource, initEvent, token) =>
        {
            var log = initEvent.Logger;
            var eventing = initEvent.Eventing;
            var notification = initEvent.Notifications;
            var services = initEvent.Services;

            // 1. Notify that we are starting
            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, services), token);
            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource.TickHand, services), token);
            await eventing.PublishAsync(new BeforeResourceStartedEvent(resource.TockHand, services), token);

            log.LogInformation("Starting Talking Clock...");

            // 2. Set initial running state
            await notification.PublishUpdateAsync(resource, s => s with
            {
                StartTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.Running
            });
            
            // ... (Set initial state for hands) ...

            // 3. The Main Loop
            while (!token.IsCancellationRequested)
            {
                log.LogInformation("The time is {time}", DateTime.UtcNow);

                // TICK
                await notification.PublishUpdateAsync(resource,
                    s => s with { State = new ResourceStateSnapshot("Tick", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TickHand,
                    s => s with { State = new ResourceStateSnapshot("On", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TockHand,
                    s => s with { State = new ResourceStateSnapshot("Off", KnownResourceStateStyles.Info) });

                await Task.Delay(1000, token);

                // TOCK
                await notification.PublishUpdateAsync(resource,
                    s => s with { State = new ResourceStateSnapshot("Tock", KnownResourceStateStyles.Success) });
                await notification.PublishUpdateAsync(resource.TickHand,
                    s => s with { State = new ResourceStateSnapshot("Off", KnownResourceStateStyles.Info) });
                await notification.PublishUpdateAsync(resource.TockHand,
                    s => s with { State = new ResourceStateSnapshot("On", KnownResourceStateStyles.Success) });

                await Task.Delay(1000, token);
            }
        });
```

**Key Takeaways:**
*   **`OnInitializeResource`**: This callback gives you access to the `ResourceNotificationService` and `ResourceLoggerService`.
*   **`PublishUpdateAsync`**: This is how you push real-time updates to the dashboard. You can change the state text ("Tick", "Tock") and the style (Success/Green, Info/Blue).
*   **`log.LogInformation`**: Logs written here appear in the "Console Logs" tab for the resource in the dashboard.

### Step 4: Use the Resource

Finally, add the resource to your `Program.cs`:

```csharp
builder.AddTalkingClock("talking-clock");
```

Run the application (`aspire run`) and observe the "talking-clock" in the dashboard. Watch its state flip between "Tick" and "Tock" and check the console logs!

---

## Part 2: Integration Testing

Aspire makes it easy to test your distributed application using the `Aspire.Hosting.Testing` package. This allows you to spin up the AppHost (or a subset of it) in a test environment.

### The Test Project

We have added a `Bookstore.AppHost.Tests` project.

### Writing a Test

Here is an example test (`WebTests.cs`) that verifies the web frontend is reachable:

```csharp
[Fact]
public async Task GetWebResourceRootReturnsOkStatusCode()
{
    // 1. Create the AppHost builder for testing
    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Bookstore_AppHost>();
    
    // 2. Configure test services (e.g., HTTP client resilience)
    appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    {
        clientBuilder.AddStandardResilienceHandler();
    });

    // 3. Build and Start the app
    await using var app = await appHost.BuildAsync();
    var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
    await app.StartAsync();

    // 4. Wait for the resource to be ready
    await resourceNotificationService.WaitForResourceAsync("web", KnownResourceStates.Running)
                                     .WaitAsync(TimeSpan.FromSeconds(30));

    // 5. Act and Assert
    var httpClient = app.CreateHttpClient("web");
    var response = await httpClient.GetAsync("/");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

**Key Concepts:**
*   **`DistributedApplicationTestingBuilder`**: A special builder for test scenarios.
*   **`WaitForResourceAsync`**: Crucial for integration tests. It ensures the target resource is up and running before you try to interact with it.
*   **`CreateHttpClient`**: Creates a pre-configured HTTP client that knows how to resolve the service discovery names (e.g., `http://web`).

### Running Tests

Run the tests using the dotnet CLI:

```bash
dotnet test
```

### Assignments

Now it's your turn! Try to implement the following tests in `Bookstore.AppHost.Tests`:

1.  **API Health Check**: Write a test that verifies the `api` resource is healthy.
    *   *Hint*: Use `WaitForResourceAsync` to wait for the `api` to be `Running`.
    *   *Hint*: The API exposes a `/health` endpoint (if configured) or you can check the root `/`.

2.  **Talking Clock State**: Write a test that verifies the `talking-clock` resource reaches the `Running` state.
    *   *Hint*: You can use `WaitForResourceAsync("talking-clock", KnownResourceStates.Running)`.

3.  **Frontend to API Connectivity**: (Advanced) Write a test that verifies the `web` frontend can successfully communicate with the `api`.
    *   *Hint*: This might involve checking a page on the frontend that requires data from the API (like the home page which lists books).

---

## Summary

In this lesson, you went beyond simply consuming resources and learned how to **extend** Aspire. You built a custom resource with its own lifecycle and state management, and you learned how to verify your application with integration tests.
