#:sdk Aspire.AppHost.Sdk@13.1.0

using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddResource(new EmptyResource("dummy"))
    .WithCommand("some-cmd", "A Command",  async context => { return new ExecuteCommandResult { Success = true }; } )
    .WithEndpoint("http", (ep)=>{})
    .WithEnvironment("key","value");

cache.Resource.Annotations.ToList().ForEach(ann => Console.WriteLine(ann.ToString()));

cache.WithEnvironment( envCallbackCtx =>
{
    // Some annotations callbacks will only be called after the DCP starts
    Debugger.Break(); 
});

builder.Build().Run();

class EmptyResource(string name) : ExecutableResource(name, "echo", ".");