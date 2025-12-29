using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.AppHost;

public static class ApiCommandExtensions
{
    public static IResourceBuilder<ProjectResource> WithSeedCommand(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithCommand(
            name: "seed-db",
            displayName: "Seed Database",
            executeCommand: async context =>
            {
                try
                {

                    builder.Resource.TryGetUrls(out var urls);
                    var url = urls?.FirstOrDefault(u => u?.Endpoint?.EndpointName == "http")?.Url
                           ?? urls?.FirstOrDefault(u => u?.Endpoint?.EndpointName == "https")?.Url;

                    if (string.IsNullOrEmpty(url))
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = "Could not determine API URL." };
                    }

                    var client = new HttpClient();
                    var response = await client.PostAsync($"{url}/seed", null);

                    if (response.IsSuccessStatusCode)
                    {
                        return new ExecuteCommandResult { Success = true };
                    }
                    else
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = $"Failed to seed database. Status code: {response.StatusCode}" };
                    }
                }
                catch (Exception ex)
                {
                    return new ExecuteCommandResult { Success = false, ErrorMessage = $"Error seeding database: {ex.Message}" };
                }
            },
            commandOptions: new CommandOptions
            {
                UpdateState = context => context.ResourceSnapshot.State == "Running" ? ResourceCommandState.Enabled : ResourceCommandState.Disabled,
                IconName = "Database", // Specify the icon name
                IconVariant = IconVariant.Filled // Specify the icon variant
            });

        return builder;
    }

    public static IResourceBuilder<ProjectResource> WithSeedHttpCommand(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithHttpCommand(
            path: "/seed",
            displayName: "Seed Database (HTTP)",
            commandOptions: new HttpCommandOptions()
            {
                Description = """
                Add books to the DB
                """,
                PrepareRequest = (context) =>
                {                   
                    context.Request.Headers.Add("X-Example-Header", $"SomeValue");
                    return Task.CompletedTask;
                },
                IconName = "DocumentLightning",
                IsHighlighted = true,            
                UpdateState = context => context.ResourceSnapshot.State == "Running" ? ResourceCommandState.Enabled : ResourceCommandState.Disabled,
                IconVariant = IconVariant.Filled // Specify the icon variant
            });

        return builder;
    }
}
