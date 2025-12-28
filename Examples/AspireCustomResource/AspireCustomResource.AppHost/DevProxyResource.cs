using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core.Tokens;

namespace AspireCustomResource.AppHost;

/// <summary>
/// Typed resource for Microsoft Dev Proxy.
/// Implements IResourceWithServiceDiscovery so other resources can call .WithReference(devProxy).
/// </summary>
public sealed class DevProxyResource(string name, string command, string workingDirectory)
    : ExecutableResource(name, command, workingDirectory), IResourceWithServiceDiscovery
{
    public const string ProxyEndpointName = "proxy";
    public const string ApiEndpointName = "api";

    public EndpointReference ProxyEndpoint => this.GetEndpoint(ProxyEndpointName);

    public string WorkDir => WorkingDirectory;

    public string GeneratedConfigFileName { get; internal set; } = "devproxyrc.generated.json";
    public string GeneratedMocksFileName { get; internal set; } = "mocks.generated.json";

    internal string GeneratedConfigPath => Path.Combine(WorkDir, GeneratedConfigFileName);
    internal string GeneratedMocksPath => Path.Combine(WorkDir, GeneratedMocksFileName);
}

public sealed record DevProxyOptions
{
    public string WorkingDirectory { get; init; } = ".";

    /// <summary>Optional base config to merge. If it exists, we merge urlsToWatch + mocks plugin into it.</summary>
    public string BaseConfigFile { get; init; } = "devproxyrc.json";

    public int Port { get; init; } = 8000;
    public int ApiPort { get; init; } = 8897;

    public string IpAddress { get; init; } = "127.0.0.1";
    public string LogLevel { get; init; } = "information";
    public bool AsSystemProxy { get; init; } = false;
    public bool InstallCert { get; init; } = false;

    /// <summary>Seed urlsToWatch in addition to AddUrlMock calls.</summary>
    public List<string> UrlsToWatch { get; init; } = [];
}

internal sealed record DevProxyOptionsAnnotation(DevProxyOptions Options) : IResourceAnnotation;
internal sealed record DevProxyUrlToWatchAnnotation(string Url) : IResourceAnnotation;
internal sealed record DevProxyMockEntryAnnotation(MockResponseEntry Entry) : IResourceAnnotation;

public static class DevProxyHostingExtensions
{
    public static IResourceBuilder<DevProxyResource> AddMicrosoftDevProxy(
        this IDistributedApplicationBuilder builder,
        string name,
        DevProxyOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        var baseDir = options.WorkingDirectory == "."
            ? builder.AppHostDirectory
            : Path.GetFullPath(options.WorkingDirectory);

        var workingDirAbs = Path.Combine(baseDir, ".aspire", "devproxy", name);
        Directory.CreateDirectory(workingDirAbs);

        var command = ResolveDevProxyCommand();

        var resource = new DevProxyResource(name, command, workingDirAbs);
        var devProxy = builder.AddResource(resource)
                               .WithInitialState(new()
                                {
                                    ResourceType = "DevProxy",
                                    CreationTimeStamp = DateTime.UtcNow,
                                    State = KnownResourceStates.NotStarted,
                                    Properties = [
                                        new(CustomResourceKnownProperties.Source, "DevProxy")
                                    ]
                                })
                              .WithAnnotation(new DevProxyOptionsAnnotation(options));

       
        devProxy.OnInitializeResource(async (devProxyResource, initEvent, ct) =>
        {
            await ConfigureDevProxyAsync(devProxyResource, options, initEvent, ct);
        });

        var generatedConfigPath = Path.Combine(workingDirAbs, "devproxyrc.generated.json");

        devProxy.WithArgs(
            "--config-file", generatedConfigPath,
            "--port", options.Port.ToString(),
            "--log-level", options.LogLevel,
            "--as-system-proxy", options.AsSystemProxy ? "true" : "false",
            "--install-cert", options.InstallCert ? "true" : "false",
            "--no-first-run",
            "--ip-address", options.IpAddress
        );

        devProxy
            .WithHttpEndpoint(port: options.Port, targetPort: options.Port, name: DevProxyResource.ProxyEndpointName, isProxied: false)
            .WithHttpEndpoint(port: options.ApiPort, targetPort: options.ApiPort, name: DevProxyResource.ApiEndpointName, isProxied: false);

        return devProxy;
    }

    private static async Task ConfigureDevProxyAsync(DevProxyResource devProxy, DevProxyOptions options, InitializeResourceEvent initEvent, CancellationToken ct)
    {
        var log = initEvent.Logger;
        var eventing = initEvent.Eventing;
        var notification = initEvent.Notifications;
        var services = initEvent.Services;

        await eventing.PublishAsync(new BeforeResourceStartedEvent(devProxy, services), ct);


        Directory.CreateDirectory(devProxy.WorkDir);
        log.LogTrace("Current working directory {workDir}", devProxy.WorkDir);

        await notification.PublishUpdateAsync(devProxy, s => s with
        {
            StartTimeStamp = DateTime.UtcNow,
            State = "Collecting Mocks"
        });
        // 1. Collect Mocks (from annotations)
        var allMocks = CollectMocks(devProxy);
        await Task.Delay(30000);
        await notification.PublishUpdateAsync(devProxy, s => s with
        {
            StartTimeStamp = DateTime.UtcNow,
            State = "Materialize Mocks"
        });
        // 2. Materialize mocks
        var mocksPath = MaterializeMocks(devProxy.WorkDir, allMocks);

        await notification.PublishUpdateAsync(devProxy, s => s with
        {
            StartTimeStamp = DateTime.UtcNow,
            State = "Collect Urls To Watch"
        });
        // 3. Collect URLs to watch
        var urlsToWatch = CollectUrlsToWatch(devProxy, options);

        // 4. Load or create config
        var config = LoadBaseConfig(options.BaseConfigFile, devProxy.WorkDir);

        // 5. Merge URLs
        MergeUrlsToWatch(config, urlsToWatch);

        // 6. Configure Mock Plugin
        if (!string.IsNullOrWhiteSpace(mocksPath))
        {
            ConfigureMockPlugin(config, mocksPath);
        }

        // 7. Write generated config
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            devProxy.GeneratedConfigPath,
            JsonSerializer.Serialize(config, jsonOptions),
            Encoding.UTF8);

        await notification.PublishUpdateAsync(devProxy, s => s with
        {
            StartTimeStamp = DateTime.UtcNow,
            State = KnownResourceStates.Running
        });
    }

    private static List<MockResponseEntry> CollectMocks(DevProxyResource devProxy)
    {
        var mocks = new List<MockResponseEntry>();

        // From annotations (added via AddUrlMock builder)
        foreach (var ann in devProxy.Annotations.OfType<DevProxyMockEntryAnnotation>())
        {
            mocks.Add(ann.Entry);
        }

        return mocks;
    }

    private static HashSet<string> CollectUrlsToWatch(DevProxyResource devProxy, DevProxyOptions options)
    {
        var urlsToWatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var u in options.UrlsToWatch)
        {
            if (!string.IsNullOrWhiteSpace(u)) urlsToWatch.Add(u.Trim());
        }

        foreach (var ann in devProxy.Annotations.OfType<DevProxyUrlToWatchAnnotation>())
        {
            if (!string.IsNullOrWhiteSpace(ann.Url)) urlsToWatch.Add(ann.Url.Trim());
        }

        return urlsToWatch;
    }

    private static DevProxyConfiguration LoadBaseConfig(string baseConfigFile, string workDir)
    {
        var baseConfigPath = ResolvePath(baseConfigFile, workDir);
        if (File.Exists(baseConfigPath))
        {
            try
            {
                var json = File.ReadAllText(baseConfigPath, Encoding.UTF8);
                return JsonSerializer.Deserialize<DevProxyConfiguration>(json) ?? new DevProxyConfiguration();
            }
            catch
            {
                return new DevProxyConfiguration();
            }
        }
        return new DevProxyConfiguration();
    }

    private static void MergeUrlsToWatch(DevProxyConfiguration config, HashSet<string> urlsToWatch)
    {
        var existingUrls = new HashSet<string>(config.UrlsToWatch, StringComparer.OrdinalIgnoreCase);

        foreach (var u in urlsToWatch)
        {
            if (existingUrls.Add(u))
            {
                config.UrlsToWatch.Add(u);
            }
        }
    }

    private static void ConfigureMockPlugin(DevProxyConfiguration config, string mocksPath)
    {
        var hasMock = config.Plugins.Any(p =>
            string.Equals(p.Name, "MockResponsePlugin", StringComparison.OrdinalIgnoreCase));

        if (!hasMock)
        {
            config.Plugins.Add(new DevProxyPlugin
            {
                Name = "MockResponsePlugin",
                Enabled = true,
                PluginPath = "~appFolder/plugins/DevProxy.Plugins.dll",
                ConfigSection = "mocksPlugin"
            });
        }

        config.MocksPlugin = new MockResponsePluginConfig
        {
            // keep portable: relative filename in WorkDir
            MocksFile = Path.GetFileName(mocksPath)
        };
    }

    /// <summary>
    /// Add a URL mock resource as an ExternalServiceResource (sealed type) and make it a child in the dashboard.
    /// Also ensures the URL gets appended into urlsToWatch of devproxyrc.generated.json at startup.
    /// Optionally configure mock responses for this URL using a fluent builder.
    /// </summary>
    public static IResourceBuilder<ExternalServiceResource> AddUrlMock(
        this IResourceBuilder<DevProxyResource> devProxy,
        string name,
        string url,
        string urlPattern,
        Action<MockResponseListBuilder>? configureMocks = null)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid absolute URL: '{url}'", nameof(url));

        // Store URL for config generation later
        devProxy.WithAnnotation(new DevProxyUrlToWatchAnnotation(urlPattern));

        // If mock configuration is provided, build it and store it
        if (configureMocks != null)
        {
            var listBuilder = new MockResponseListBuilder();
            configureMocks(listBuilder);
            var entries = listBuilder.Build();

            foreach (var entry in entries)
            {
                // If the user didn't specify a URL in the mock entry, default to the pattern
                if (string.IsNullOrWhiteSpace(entry.Request.Url))
                {
                    entry.Request.Url = urlPattern;
                }
                devProxy.WithAnnotation(new DevProxyMockEntryAnnotation(entry));
            }
        }

        // Create the external resource (cannot derive from ExternalServiceResource)
        var ext = devProxy.ApplicationBuilder.AddExternalService(name, uri.ToString());

        // Make it appear as a child of Dev Proxy in the dashboard
        ext.WithParentRelationship(devProxy.Resource);

        return ext;
    }

    /// <summary>
    /// - Wait for Dev Proxy
    /// - Apply proxy env vars on the consumer via deferred environment callback
    /// </summary>
    public static IResourceBuilder<T> WithDevProxy<T>(
        this IResourceBuilder<T> consumer,
        IResourceBuilder<DevProxyResource> devProxy,
        string? noProxy = null)
        where T : IResourceWithEnvironment
    {
        return consumer.WithEnvironment(ctx =>
        {
            var proxyUrl = devProxy.Resource.GetEndpoint(DevProxyResource.ProxyEndpointName).Url;
            ctx.EnvironmentVariables["HTTP_PROXY"] = proxyUrl;
            ctx.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
            ctx.EnvironmentVariables["NO_PROXY"] = noProxy ?? "localhost,127.0.0.1,::1";
        });
    }

    private static string ResolveDevProxyCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var p1 = Path.Combine(localAppData, "Programs", "Dev Proxy", "devproxy.exe");
            if (File.Exists(p1)) return p1;

            var p2 = Path.Combine(localAppData, "Microsoft", "WindowsApps", "devproxy.exe");
            if (File.Exists(p2)) return p2;

            return "devproxy.exe";
        }

        return "devproxy";
    }

    private static string? MaterializeMocks(string workingDirectoryAbsolute, List<MockResponseEntry> allMocks)
    {
        // If we have no mocks at all (neither from options nor annotations), return null
        if (allMocks.Count == 0)
            return null;

        Directory.CreateDirectory(workingDirectoryAbsolute);

        var fileName = "mocks.generated.json";
        var destPath = Path.Combine(workingDirectoryAbsolute, fileName);
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        // Priority 1: Collected Mocks (from annotations)
        var config = new MockResponseConfiguration { Mocks = allMocks };
        var json = JsonSerializer.Serialize(config, jsonOptions);
        File.WriteAllText(destPath, json, Encoding.UTF8);
        return destPath;
    }

    private static string ResolvePath(string path, string baseDir)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDir, path));
}
