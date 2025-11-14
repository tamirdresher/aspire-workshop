// Welcome to .NET Aspire!
// This is the simplest possible Aspire application.

var builder = DistributedApplication.CreateBuilder(args);

// The AppHost orchestrates your distributed application
// Right now, there are no services or resources defined
// But the dashboard will still open and show the orchestration system

Console.WriteLine("🚀 Hello from Aspire!");
Console.WriteLine("📊 The dashboard will open automatically");
Console.WriteLine("🌐 Usually at: http://localhost:15888");

builder.Build().Run();
