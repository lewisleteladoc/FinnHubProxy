/*
 * This version:

Works locally

Works in Docker

Enables Swagger everywhere

Avoids HTTPS issues

Binds correctly inside containers
*/

using FinnHubProxy.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔑 VERY IMPORTANT for Docker
var configuration = builder.Configuration;

if (configuration["IS_DOCKER"] == "true")
{
    Console.WriteLine(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
    builder.WebHost.UseUrls("http://0.0.0.0:8080");
}


// Add services to the container.

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddSingleton<WatchListStore>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DO NOT redirect HTTPS in containers
var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

if (!runningInContainer)
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok("FinnHubProxy is running"));

app.Run();
