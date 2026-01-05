using System.Text.Json.Serialization;

namespace AspireCustomResource.AppHost;

// --- Configuration Models (devproxyrc.json) ---

public class DevProxyConfiguration
{
    [JsonPropertyName("plugins")]
    public List<DevProxyPlugin> Plugins { get; set; } = [];

    [JsonPropertyName("urlsToWatch")]
    public List<string> UrlsToWatch { get; set; } = [];

    [JsonPropertyName("mocksPlugin")]
    public MockResponsePluginConfig? MocksPlugin { get; set; }

    [JsonPropertyName("logLevel")]
    public string? LogLevel { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = [];
}

public class DevProxyPlugin
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("pluginPath")]
    public string PluginPath { get; set; } = "";

    [JsonPropertyName("configSection")]
    public string? ConfigSection { get; set; }
}

public class MockResponsePluginConfig
{
    [JsonPropertyName("mocksFile")]
    public string MocksFile { get; set; } = "mocks.json";
}

// --- Mock Models (mocks.json) ---

public class MockResponseConfiguration
{
    [JsonPropertyName("mocks")]
    public List<MockResponseEntry> Mocks { get; set; } = [];
}

public class MockResponseEntry
{
    [JsonPropertyName("request")]
    public MockRequestMatcher Request { get; set; } = new();

    [JsonPropertyName("response")]
    public MockResponseAction Response { get; set; } = new();
}

public class MockRequestMatcher
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";
}

public class MockResponseAction
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MockHeader>? Headers { get; set; }
}

public class MockHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

// --- Fluent Builder ---

public class MockRequestBuilder
{
    private readonly MockResponseEntry _entry = new();

    public MockRequestBuilder WithUrl(string url)
    {
        _entry.Request.Url = url;
        return this;
    }

    public MockRequestBuilder WithMethod(string method)
    {
        _entry.Request.Method = method;
        return this;
    }

    public MockRequestBuilder WithStatusCode(int statusCode)
    {
        _entry.Response.StatusCode = statusCode;
        return this;
    }

    public MockRequestBuilder WithBody(object body)
    {
        _entry.Response.Body = body;
        return this;
    }

    public MockRequestBuilder WithHeader(string name, string value)
    {
        _entry.Response.Headers ??= [];
        _entry.Response.Headers.Add(new MockHeader { Name = name, Value = value });
        return this;
    }

    public MockResponseEntry Build() => _entry;
}

public class MockResponseListBuilder
{
    private readonly List<MockResponseEntry> _entries = [];

    public MockRequestBuilder Add()
    {
        var builder = new MockRequestBuilder();
        // We defer adding to _entries until Build() is called on the MockRequestBuilder?
        // No, that's tricky. Let's make Add return the builder, and we need a way to capture the result.
        // A common pattern is Add(Action<MockRequestBuilder>)
        return builder;
    }

    public MockResponseListBuilder Add(Action<MockRequestBuilder> configure)
    {
        var builder = new MockRequestBuilder();
        configure(builder);
        _entries.Add(builder.Build());
        return this;
    }

    public List<MockResponseEntry> Build() => _entries;
}
