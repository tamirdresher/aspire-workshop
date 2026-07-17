using Microsoft.Extensions.Diagnostics.HealthChecks;

#pragma warning disable ASPIREINTERACTION001

namespace Bookstore.AppHost;

public static class ApiCommandExtensions
{
    private static readonly HttpClient Client = new();

    public static IResourceBuilder<ProjectResource> WithSeedCommand(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithCommand(
            name: "seed-db",
            displayName: "Seed Database",
            executeCommand: async context =>
            {
                var apiEndpoint = GetPreferredApiEndpoint(builder);
                if (apiEndpoint is null)
                {
                    return CommandResults.Failure(
                        $"Resource '{builder.Resource.Name}' does not expose an HTTP(S) endpoint. " +
                        "Configure an endpoint named 'http' or 'https', or set an endpoint URI scheme to HTTP(S).");
                }

                var endpointUrl = await apiEndpoint.GetValueAsync(context.CancellationToken);
                if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint) ||
                    (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                {
                    return CommandResults.Failure(
                        $"The '{apiEndpoint.EndpointName}' endpoint for resource '{builder.Resource.Name}' " +
                        "did not resolve to a valid absolute HTTP(S) URL. Ensure the API resource is running.");
                }

                try
                {
                    using var response = await Client.PostAsync(
                        new Uri(endpoint, "/seed"),
                        null,
                        context.CancellationToken);
                    var responseText = await response.Content.ReadAsStringAsync(context.CancellationToken);

                    return response.IsSuccessStatusCode
                        ? CommandResults.Success(
                            message: "Database seed request completed.",
                            result: responseText,
                            resultFormat: CommandResultFormat.Text,
                            displayImmediately: context.Arguments.GetBoolean("show-response"))
                        : CommandResults.Failure(
                            errorMessage: $"Database seed request failed with status {(int)response.StatusCode}.",
                            result: responseText,
                            resultFormat: CommandResultFormat.Text);
                }
                catch (HttpRequestException ex)
                {
                    return CommandResults.Failure(ex);
                }
            },
            commandOptions: new CommandOptions
            {
                Description = "Seeds the catalog and optionally opens the API response.",
                ConfirmationMessage = "Seed the catalog database?",
                Arguments =
                [
                    new InteractionInput
                    {
                        Name = "show-response",
                        Label = "Open response",
                        InputType = InputType.Boolean,
                        Value = "true",
                        Required = true
                    }
                ],
                UpdateState = GetCommandState,
                Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
                IconName = "Database",
                IconVariant = IconVariant.Filled
            });

        return builder;
    }

    private static EndpointReference? GetPreferredApiEndpoint(
        IResourceBuilder<ProjectResource> builder)
    {
        var httpEndpoint = builder.GetEndpoint("http");
        if (httpEndpoint.Exists)
        {
            return httpEndpoint;
        }

        var httpsEndpoint = builder.GetEndpoint("https");
        if (httpsEndpoint.Exists)
        {
            return httpsEndpoint;
        }

        if (!builder.Resource.TryGetEndpoints(out var endpoints))
        {
            return null;
        }

        var endpointList = endpoints.ToArray();
        var endpoint = endpointList.FirstOrDefault(
            static endpoint => string.Equals(
                endpoint.UriScheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase))
            ?? endpointList.FirstOrDefault(
                static endpoint => string.Equals(
                    endpoint.UriScheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase));

        return endpoint is null ? null : builder.GetEndpoint(endpoint.Name);
    }

    public static IResourceBuilder<ProjectResource> WithSeedHttpCommand(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithHttpCommand(
            path: "/seed",
            displayName: "Seed Database (HTTP)",
            commandOptions: new HttpCommandOptions
            {
                Description = "Seeds the catalog through a POST request and displays the response body.",
                ConfirmationMessage = "Seed the catalog database?",
                Method = HttpMethod.Post,
                ResultMode = HttpCommandResultMode.Text,
                IconName = "DocumentLightning",
                IconVariant = IconVariant.Filled,
                IsHighlighted = true,
                UpdateState = GetCommandState,
                Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api
            });

        return builder;
    }

    private static ResourceCommandState GetCommandState(UpdateCommandStateContext context) =>
        context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled;
}
