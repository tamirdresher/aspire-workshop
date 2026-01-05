using Aspire.Hosting;
using AspireCustomResource.AppHost;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using static System.Net.WebRequestMethods;

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var cache = builder.AddRedis("cache")
    .WithoutHttpsCertificate()    ;
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


var devProxy = builder.AddMicrosoftDevProxy(
    name: "devproxy",
    options: new DevProxyOptions
    {
        WorkingDirectory = builder.AppHostDirectory,
        BaseConfigFile = "devproxyrc.json", // optional; will be merged if exists
        Port = 18000,
        ApiPort = 8897
    });

var example = devProxy.AddUrlMock("example",
    urlPattern: "http://example.com/*",
    url: "http://example.com",
    configureMocks: mocks =>
    {
        mocks.Add(mock => mock
            .WithUrl("http://example.com/data")
            .WithMethod("GET")
            .WithStatusCode(200)
            .WithHeader("content-type", "application/json; odata.metadata=minimal")
            .WithBody(new { message = "hello from inline mocks" }));

            mocks.Add(mock => mock
            .WithUrl("http://example.com/data/fail")
            .WithMethod("GET")
            .WithStatusCode(400)
            .WithHeader("content-type", "application/json; odata.metadata=minimal")
            .WithBody(new { message = "example pf failing from inline mocks" }));
    });

var apiService = builder.AddProject<Projects.AspireCustomResource_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")   
    .WaitFor(devProxy)
    .WithReference(example)
    .WithReference(devProxy)
    .WithDevProxy(devProxy)
    .WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/example-data", DisplayText = "example.com", DisplayLocation = UrlDisplayLocation.SummaryAndDetails })
    .WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/example-data?fail=true", DisplayText = "failing example.com", DisplayLocation = UrlDisplayLocation.SummaryAndDetails });

builder.AddProject<Projects.AspireCustomResource_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(devProxy);

builder.Build().Run();
