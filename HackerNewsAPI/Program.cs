using HackerNewsAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
// Add services to the container.
builder.Services.AddMemoryCache(); // For in-memory caching
builder.Services.AddResponseCaching(); // Add response caching services
builder.Services.AddHttpClient("HackerNewsHttpClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set the timeout duration
});
builder.Services.AddControllers();
builder.Services.AddTransient<IStoryService, StoryService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HackerNews API",
        Version = "v1",
        Description = "HackerNews API extention"
    });
});
// Read the AllowSpecificOrigin configuration.
var allowSpecificOrigin = builder.Configuration.GetValue<string>("AllowSpecificOrigin");
// Configure CORS to use the AllowSpecificOrigin setting.
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpecificOriginPolicy", policy =>
    {
        policy.WithOrigins(allowSpecificOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();
// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
// specifying the Swagger JSON endpoint.
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HackerNews API");
    c.RoutePrefix = "swagger/ui";
});

app.UseRouting();

app.UseCors("SpecificOriginPolicy");

app.UseHttpsRedirection();

app.UseResponseCaching();

app.MapControllers();

app.Run();
