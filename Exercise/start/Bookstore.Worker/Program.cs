using Bookstore.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<Worker>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7032");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
