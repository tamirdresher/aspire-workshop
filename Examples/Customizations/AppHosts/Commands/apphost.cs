#:sdk Aspire.AppHost.Sdk@13.4.6
#:property ManagePackageVersionsCentrally=false

#:package Aspire.Hosting.Redis@13.4.6
#:package StackExchange.Redis@2.13.1

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIRECERTIFICATES001

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate()
    .WithLifetime(ContainerLifetime.Persistent);

var commandArguments = new InteractionInput[]
{
    new()
    {
        Name = "database",
        Label = "Database",
        InputType = InputType.Number,
        Value = "0",
        Required = true
    },
    new()
    {
        Name = "mode",
        Label = "Flush mode",
        InputType = InputType.Choice,
        Value = "async",
        Required = true,
        Options =
        [
            new("async", "Asynchronous"),
            new("sync", "Synchronous")
        ]
    },
    new()
    {
        Name = "show-result",
        Label = "Open result",
        InputType = InputType.Boolean,
        Value = "true",
        Required = true
    }
};

static Task ValidateArguments(InputsDialogValidationContext context)
{
    var databaseNumber = context.Inputs.GetInt32("database");
    if (databaseNumber is < 0 or > 15)
    {
        context.AddValidationError("database", "Database must be between 0 and 15.");
    }

    return Task.CompletedTask;
}

cache.WithCommand(
    name: "clear-cache",
    displayName: "Clear Cache",
    executeCommand: async context =>
    {
        var databaseNumber = context.Arguments.GetInt32("database");
        var flushMode = context.Arguments.GetString("mode")!;
        var showResult = context.Arguments.GetBoolean("show-result");
        var connectionString = await cache.Resource.GetConnectionStringAsync();

        if (connectionString is null)
        {
            return CommandResults.Failure(
                $"The connection string for '{context.ResourceName}' is unavailable.");
        }

        try
        {
            using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var server = connection.GetServer(connection.GetEndPoints()[0]);
            var database = connection.GetDatabase(databaseNumber);
            var entriesBefore = await server.DatabaseSizeAsync(databaseNumber);

            await database.ExecuteAsync("FLUSHDB", flushMode.ToUpperInvariant());

            var result = JsonSerializer.Serialize(new
            {
                resource = context.ResourceName,
                database = databaseNumber,
                mode = flushMode,
                entriesRemoved = entriesBefore,
                completedAt = DateTimeOffset.UtcNow
            });

            return CommandResults.Success(
                message: $"Redis database {databaseNumber} was cleared.",
                result: result,
                resultFormat: CommandResultFormat.Json,
                displayImmediately: showResult);
        }
        catch (RedisException ex)
        {
            context.Logger.LogError(ex, "Failed to clear Redis database {Database}.", databaseNumber);
            return CommandResults.Failure(ex);
        }
    },
    commandOptions: new CommandOptions
    {
        Description = "Clears one Redis database and returns a JSON summary.",
        ConfirmationMessage = "Clear the selected Redis database? This cannot be undone.",
        Arguments = commandArguments,
        ValidateArguments = ValidateArguments,
        UpdateState = context =>
            context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                ? ResourceCommandState.Enabled
                : ResourceCommandState.Disabled,
        Visibility = ResourceCommandVisibility.UI | ResourceCommandVisibility.Api,
        IconName = "AnimalRabbitOff",
        IconVariant = IconVariant.Filled,
        IsHighlighted = true
    });

cache.WithCommand(
    name: "preview-clear-cache",
    displayName: "Preview Clear Cache",
    executeCommand: context =>
    {
        var result = JsonSerializer.Serialize(new
        {
            resource = context.ResourceName,
            database = context.Arguments.GetInt32("database"),
            mode = context.Arguments.GetString("mode"),
            showResult = context.Arguments.GetBoolean("show-result")
        });

        return Task.FromResult(CommandResults.Success(
            message: "Clear-cache options validated.",
            result: result,
            resultFormat: CommandResultFormat.Json));
    },
    commandOptions: new CommandOptions
    {
        Description = "Validates clear-cache options without changing Redis data.",
        Arguments = commandArguments,
        ValidateArguments = ValidateArguments,
        UpdateState = _ => ResourceCommandState.Enabled,
        Visibility = ResourceCommandVisibility.Api,
        IconName = "DocumentSearch",
        IconVariant = IconVariant.Regular
    });

var apiService = builder.AddCSharpApp("api", "../../../Services/AspireCustomResource.ApiService/")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddCSharpApp("frontend", "../../../Services/AspireCustomResource.Web/")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();