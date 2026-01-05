#:sdk Aspire.AppHost.Sdk@13.1.0

#:package Aspire.Hosting.Redis@13.1.0

using Microsoft.Extensions.DependencyInjection;

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate()
    .WithCommand("clear-cache", "Clear Cache",
        async context => {
            var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
            if (interactionService.IsAvailable)
            {
                var result = await interactionService.PromptConfirmationAsync(
                    title: "Clear confirmation",
                    message: "Are you sure you want to delete the data?");

                if (result.Data)
                {
                    // Run your resource/command logic.
                }
            }
            return new ExecuteCommandResult { Success = true, ErrorMessage = "" };
           },
        commandOptions: new CommandOptions
        {
            UpdateState = updateCtx=>ResourceCommandState.Enabled,
            IconName = "AnimalRabbitOff", // Specify the icon name
            IconVariant = IconVariant.Filled // Specify the icon variant
        });

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cache)
    .WaitFor(cache);

var web = builder.AddCSharpApp("frontend", "../../../Services/AspireCustomResource.Web/")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();