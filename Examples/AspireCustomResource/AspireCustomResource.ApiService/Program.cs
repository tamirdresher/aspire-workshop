

using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var redisConn = builder.Configuration.GetConnectionString("cache");
builder.Services.AddHealthChecks().AddRedis(redisConn!);

builder.Services.AddHttpClient<ExampleApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("http://example");
});

// Build the main app (API endpoints)
var app = builder.Build();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data. or /health-ui to see the health-ui");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


app.MapGet("/example-data", async (bool? fail, ExampleApiClient client) =>
{
    if (fail == true)
    {
        return await client.GetFailDataAsync();
    }
    return await client.GetDataAsync();
})
.WithName("GetExampleData");

app.MapDefaultEndpoints();

// Start the main app (API endpoints) in a background task
var mainAppTask = Task.Run(() => app.Run());


var adminPort = builder.Configuration.GetValue<int?>("ADMIN_PORT");
// If adminPort is set, start a second web server for HealthCheck UI endpoints
if (adminPort is not null)
{
    var adminBuilder = WebApplication.CreateBuilder(args);
    adminBuilder.WebHost.UseUrls($"http://*:{adminPort.Value}");
    adminBuilder.Services.AddHealthChecksUI(setup =>
    {
        setup.AddHealthCheckEndpoint("self", $"http://localhost:{adminPort}/health-ui-check");
    })
    .AddInMemoryStorage();
    adminBuilder.Services.AddHealthChecks().AddRedis(redisConn!);

    var adminApp = adminBuilder.Build();

    // Only map HealthCheck UI endpoints on the admin port
    adminApp.MapHealthChecks("/health-ui-check", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    adminApp.MapHealthChecksUI(o =>
    {
        o.UIPath = "/health-ui";
        o.ApiPath = "/health-ui-api";
    });

    adminApp.Run();
}
else
{
    // If no admin port, run the main app normally (already started above)
    mainAppTask.Wait();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
