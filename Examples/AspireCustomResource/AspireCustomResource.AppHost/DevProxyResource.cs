// =======================================================
// DevProxyHostingExtensions.cs  (updated to match what we discussed)
// Changes vs your current code:
// 1) Adds AddUrlMock(...) -> returns IResourceBuilder<ExternalServiceResource> (ExternalServiceResource is sealed)
// 2) AddUrlMock annotates DevProxyResource; hook merges these into urlsToWatch
// 3) Generates devproxyrc.generated.json + mocks file at app start (BeforeStartAsync), not during build
// 4) Keeps your “run devproxy.exe directly” approach (no BuildWindowsRunScript / wrapper execution)
// =======================================================

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AspireCustomResource.AppHost;

/// <summary>
/// Typed resource for Microsoft Dev Proxy.
/// Implements IResourceWithServiceDiscovery so other resources can call .WithReference(devProxy).
/// </summary>
public sealed class DevProxyResource : ExecutableResource, IResourceWithServiceDiscovery
{
    public const string ProxyEndpointName = "proxy";
    public const string ApiEndpointName = "api";

    public DevProxyResource(string name, string command, string workingDirectory)
        : base(name, command, workingDirectory) { }

    public EndpointReference ProxyEndpoint => this.GetEndpoint(ProxyEndpointName);

    public string WorkDir => WorkingDirectory;

    public string GeneratedConfigFileName { get; internal set; } = "devproxyrc.generated.json";
    public string GeneratedMocksFileName { get; internal set; } = "mocks.generated.json";

    internal string GeneratedConfigPath => Path.Combine(WorkDir, GeneratedConfigFileName);
    internal string GeneratedMocksPath => Path.Combine(WorkDir, GeneratedMocksFileName);
}

public sealed record DevProxyMocksOptions
{
    public string? ExistingFilePath { get; init; }
    public string? JsonContent { get; init; }

    public string FileName { get; init; } = "mocks.generated.json";
    public bool ForceWrite { get; init; } = false;
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

    public DevProxyMocksOptions? Mocks { get; init; }

    /// <summary>Seed urlsToWatch in addition to AddUrlMock calls.</summary>
    public List<string> UrlsToWatch { get; init; } = new();
}

// ----------------------------
// Annotations
// ----------------------------
internal sealed record DevProxyOptionsAnnotation(DevProxyOptions Options) : IResourceAnnotation;
internal sealed record DevProxyUrlToWatchAnnotation(string Url) : IResourceAnnotation;


// ----------------------------
// Public extensions
// ----------------------------
public static class DevProxyHostingExtensions
{
    public static IResourceBuilder<DevProxyResource> AddMicrosoftDevProxy(
        this IDistributedApplicationBuilder builder,
        string name,
        DevProxyOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resource name must be provided.", nameof(name));

        // Keep your working directory logic, but pin generated files into a stable per-resource folder
        var baseDir = options.WorkingDirectory == "."
            ? builder.AppHostDirectory
            : Path.GetFullPath(options.WorkingDirectory);

        var workingDirAbs = Path.Combine(baseDir, ".aspire", "devproxy", name);
        Directory.CreateDirectory(workingDirAbs);

        // Keep your "run devproxy directly" approach (no wrapper scripts).
        // IMPORTANT: this assumes devproxy exists where we point to, or is on PATH for non-Windows.
        var command = ResolveDevProxyCommand();

        DevProxyResource resource = new(name, command, workingDirAbs);
        var devProxy = builder.AddResource(resource)
                              .WithAnnotation(new DevProxyOptionsAnnotation(options));

        devProxy.OnBeforeResourceStarted((devProxy, beforeStartEvent, ct) =>
        {
            Directory.CreateDirectory(devProxy.WorkDir);

            // 1) Materialize mocks into WorkDir (optional)
            var mocksPath = MaterializeMocks(
                workingDirectoryAbsolute: devProxy.WorkDir,
                mocksOptions: options.Mocks);

            // 2) Collect urlsToWatch from options + AddUrlMock annotations
            var urlsToWatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var u in options.UrlsToWatch)
            {
                if (!string.IsNullOrWhiteSpace(u)) urlsToWatch.Add(u.Trim());
            }

            foreach (var ann in devProxy.Annotations.OfType<DevProxyUrlToWatchAnnotation>())
            {
                if (!string.IsNullOrWhiteSpace(ann.Url)) urlsToWatch.Add(ann.Url.Trim());
            }

            // 3) Load base config if exists; else minimal
            var baseConfigPath = ResolvePath(options.BaseConfigFile, devProxy.WorkDir);

            JsonObject config;
            if (File.Exists(baseConfigPath))
            {
                config = JsonNode.Parse(File.ReadAllText(baseConfigPath, Encoding.UTF8))?.AsObject()
                         ?? new JsonObject();
            }
            else
            {
                config = new JsonObject();
            }

            // 4) Merge urlsToWatch
            var urlsArr = config["urlsToWatch"] as JsonArray ?? new JsonArray();
            config["urlsToWatch"] = urlsArr;

            var existingUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in urlsArr)
            {
                if (n is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                    existingUrls.Add(s.Trim());
            }

            foreach (var u in urlsToWatch)
            {
                if (existingUrls.Add(u))
                    urlsArr.Add(u);
            }

            // 5) Ensure MockResponsePlugin config (only if mocks are present)
            if (!string.IsNullOrWhiteSpace(mocksPath))
            {
                var plugins = config["plugins"] as JsonArray ?? new JsonArray();
                config["plugins"] = plugins;

                var hasMock = plugins.OfType<JsonObject>().Any(p =>
                    string.Equals(p["name"]?.ToString(), "MockResponsePlugin", StringComparison.OrdinalIgnoreCase));

                if (!hasMock)
                {
                    plugins.Add(new JsonObject
                    {
                        ["name"] = "MockResponsePlugin",
                        ["enabled"] = true,
                        ["pluginPath"] = "~appFolder/plugins/DevProxy.Plugins.dll",
                        ["configSection"] = "mocksPlugin"
                    });
                }

                config["mocksPlugin"] = new JsonObject
                {
                    // keep portable: relative filename in WorkDir
                    ["mocksFile"] = Path.GetFileName(mocksPath)
                };
            }

            // 6) Write generated config next to mocks in WorkDir
            File.WriteAllText(
                devProxy.GeneratedConfigPath,
                config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);

            return Task.CompletedTask;
        });

        // NOTE: config file is generated at startup, but the path is stable.
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

        static string ResolveDevProxyCommand()
        {
            if (OperatingSystem.IsWindows())
            {
                // Prefer the most common per-user winget location you already used
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var p1 = Path.Combine(localAppData, "Programs", "Dev Proxy", "devproxy.exe");
                if (File.Exists(p1)) return p1;

                // WindowsApps shim (often exists and avoids PATH refresh issues)
                var p2 = Path.Combine(localAppData, "Microsoft", "WindowsApps", "devproxy.exe");
                if (File.Exists(p2)) return p2;

                // Fallback: PATH (requires restart after first install)
                return "devproxy.exe";
            }

            // macOS / Linux: rely on PATH (brew/setup.sh usually place it there)
            return "devproxy";
        }
    }

    /// <summary>
    /// Add a URL mock resource as an ExternalServiceResource (sealed type) and make it a child in the dashboard.
    /// Also ensures the URL gets appended into urlsToWatch of devproxyrc.generated.json at startup.
    /// </summary>
    public static IResourceBuilder<ExternalServiceResource> AddUrlMock(
        this IResourceBuilder<DevProxyResource> devProxy,
        string name,
        string url,
        string urlPattern)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid absolute URL: '{url}'", nameof(url));

        // Store URL for config generation later
        devProxy.WithAnnotation(new DevProxyUrlToWatchAnnotation(urlPattern));

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


    private static string? MaterializeMocks(string workingDirectoryAbsolute, DevProxyMocksOptions? mocksOptions)
    {
        if (mocksOptions is null)
            return null;

        Directory.CreateDirectory(workingDirectoryAbsolute);

        var destPath = Path.Combine(workingDirectoryAbsolute, mocksOptions.FileName);

        if (!string.IsNullOrWhiteSpace(mocksOptions.JsonContent))
        {
            var newBytes = Encoding.UTF8.GetBytes(mocksOptions.JsonContent!);

            if (!mocksOptions.ForceWrite && File.Exists(destPath))
            {
                var oldBytes = File.ReadAllBytes(destPath);
                if (SHA256.HashData(oldBytes).AsSpan().SequenceEqual(SHA256.HashData(newBytes)))
                    return destPath;
            }

            File.WriteAllBytes(destPath, newBytes);
            return destPath;
        }

        if (!string.IsNullOrWhiteSpace(mocksOptions.ExistingFilePath))
        {
            var src = ResolvePath(mocksOptions.ExistingFilePath!, workingDirectoryAbsolute);
            if (!File.Exists(src))
                throw new FileNotFoundException($"Mocks file not found: {src}", src);

            File.Copy(src, destPath, overwrite: true);
            return destPath;
        }

        return null;
    }

    private static string ResolvePath(string path, string baseDir)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDir, path));

}
