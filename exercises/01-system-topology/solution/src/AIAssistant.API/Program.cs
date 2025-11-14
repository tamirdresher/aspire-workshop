using AIAssistant.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add chat service
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Chat endpoints
app.MapPost("/api/chat", async (ChatRequest request, ChatService chatService) =>
{
    var response = await chatService.GetChatResponseAsync(request.UserId, request.Message);
    return Results.Ok(new ChatResponse(response));
})
.WithName("SendChatMessage")
.WithOpenApi();

app.MapDelete("/api/chat/{userId}", async (string userId, ChatService chatService) =>
{
    await chatService.ClearConversationAsync(userId);
    return Results.NoContent();
})
.WithName("ClearConversation")
.WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

record ChatRequest(string UserId, string Message);
record ChatResponse(string Message);
