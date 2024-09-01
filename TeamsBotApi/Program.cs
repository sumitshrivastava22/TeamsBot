using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using TeamsBotApi.Bots;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();

builder.Services.AddControllers();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>();
builder.Services.AddTransient<IBot, TeamsBot>();

var app = builder.Build();

app.UseAuthentication();
app.MapControllers();

app.Map("/welcome", async context =>
{
	context.Response.ContentType = "application/json";
	await context.Response.WriteAsync("{\"message\": \"Welcome to the Teams Bot API. Please specify a valid endpoint.\"}");
});

app.Run();
