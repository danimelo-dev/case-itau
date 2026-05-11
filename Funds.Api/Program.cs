using Funds.Api.Extensions;
using Funds.Api.HealthChecks;
using Funds.Api.Middlewares;
using Funds.Api.Persistence;
using Funds.Api.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "Funds.Api")
        .Enrich.WithProperty(
            "Environment",
            context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] " +
            "[CorrelationId: {CorrelationId}] " +
            "[Application: {Application}] " +
            "[Environment: {Environment}] " +
            "{Message:lj}{NewLine}{Exception}");
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

var redisConnectionString =
    builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var configurationOptions =
            ConfigurationOptions.Parse(redisConnectionString);

        configurationOptions.AbortOnConnectFail = false;
        configurationOptions.ConnectRetry = 2;
        configurationOptions.ConnectTimeout = 1000;
        configurationOptions.SyncTimeout = 1000;
        configurationOptions.AsyncTimeout = 1000;

        return ConnectionMultiplexer.Connect(configurationOptions);
    });

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.ConfigurationOptions =
            ConfigurationOptions.Parse(redisConnectionString);

        options.ConfigurationOptions.AbortOnConnectFail = false;
        options.ConfigurationOptions.ConnectRetry = 2;
        options.ConfigurationOptions.ConnectTimeout = 1000;
        options.ConfigurationOptions.SyncTimeout = 1000;
        options.ConfigurationOptions.AsyncTimeout = 1000;

        options.InstanceName = "funds-api:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];
var jwtSecretKey = jwtSettings["SecretKey"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException(
        "JWT configuration is missing. Check Jwt__Issuer, Jwt__Audience and Jwt__SecretKey.");
}

var key = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var healthChecksBuilder = builder.Services
    .AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>(
        name: "sql-server",
        tags: new[] { "ready" });

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    healthChecksBuilder.AddCheck<RedisHealthCheck>(
        name: "redis",
        tags: new[] { "cache" });
}

builder.Services.AddApplicationDependencies();

var app = builder.Build();

if (app.Environment.IsDevelopment() ||
    app.Environment.IsProduction())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();

    await DatabaseSeed.SeedAsync(context);
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath}{QueryString} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Debug;
        }

        return ex is not null ||
               httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext =
        (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set(
                "RequestHost",
                httpContext.Request.Host.Value);

            diagnosticContext.Set(
                "RequestScheme",
                httpContext.Request.Scheme);

            diagnosticContext.Set(
                "QueryString",
                httpContext.Request.QueryString.Value);

            diagnosticContext.Set(
                "UserAgent",
                httpContext.Request.Headers.UserAgent.ToString());

            if (httpContext.Items.TryGetValue(
                    CorrelationIdMiddleware.CorrelationIdHeader,
                    out var correlationId))
            {
                diagnosticContext.Set(
                    "CorrelationId",
                    correlationId?.ToString());
            }
        };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();