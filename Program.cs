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
/*else
{
    builder.WebHost.UseUrls("http://localhost:9090");
}*/

var devUrl = configuration["Kestrel:Endpoints:Http:Url"];
Console.WriteLine($"Starting on URL: {devUrl}");

// builder.WebHost.UseUrls("http://localhost:9999");

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
// Add other services
builder.Services.AddSingleton<WatchListStore>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<AdminUserStore>();
builder.Services.AddSingleton<UserWatchlistStore>();
builder.Services.AddSingleton<SecuritiesStoriesStore>();
builder.Services.AddHostedService<StartupFileLoaderService>();
builder.Services.AddHostedService<SecuritiesStoriesLoaderBackgroundService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// ---- MIDDLEWARE GOES HERE ----
app.Use(async (context, next) => {
       
    // Skip auth check for Swagger, root endpoint and swagger JSON
    if (context.Request.Path.StartsWithSegments("/swagger") ||
        context.Request.Path.StartsWithSegments("/favicon") ||
        context.Request.Path == "/")
    {
        await next();
        return;
    }

    string usernameSetting = configuration["XUsername"].ToString();
    string authSetting = configuration["Authorization"].ToString();
    var username = context.Request.Headers[usernameSetting].ToString();
    var token = context.Request.Headers[authSetting].ToString();

    /*if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: missing username or token.");
        return;
    }*/

    context.Items["username"] = username;
    context.Items["token"] = token;
    await next();
});
// ---- END MIDDLEWARE ----


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

app.MapGet("/", () => Results.Ok("FinnHubProxy is running: " + DateTime.Now));

app.Run();
