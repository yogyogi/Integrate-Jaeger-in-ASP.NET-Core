using JaegerTutorial.Models;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CompanyContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<AppActivitySource>();

IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);
});

var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName));

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSqlClientInstrumentation(); // dotnet add package OpenTelemetry.Instrumentation.SqlClient
    tracing.AddEntityFrameworkCoreInstrumentation(); // dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 1.15.0-beta.1
    tracing.AddSource(AppActivitySource.ActivitySourceName); // custom Activity
    tracing.AddRedisInstrumentation(connectionMultiplexer); // dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis --version 1.0.0-rc9.15
    tracing.AddRabbitMQInstrumentation(); // dotnet add package RabbitMQ.Client.OpenTelemetry --version 1.0.0-rc.2

    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
        });
    }
    else
    {
        tracing.AddConsoleExporter();
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
