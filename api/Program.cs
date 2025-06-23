/** Program.cs **/
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.OpenApi.Models;
using FamilyBudgetApi.Services;
using FamilyBudgetApi.Controllers;
using FamilyBudgetApi.Converters;
using Microsoft.Extensions.Logging;
using FamilyBudgetApi.Logging;
using System;
using System.IO;

// Top-level statements
var builder = WebApplication.CreateBuilder(args);

// Firebase setup (pre-build logging using Console temporarily)
var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
if (string.IsNullOrEmpty(credentialsJson))
{
    Console.WriteLine("Error: GOOGLE_APPLICATION_CREDENTIALS_JSON environment variable not set.");
    throw new InvalidOperationException("Firebase credentials not provided via GOOGLE_APPLICATION_CREDENTIALS_JSON.");
}

string tempCredentialsPath = null;
try
{
    tempCredentialsPath = Path.Combine(Path.GetTempPath(), "firebase-service-account.json");
    File.WriteAllText(tempCredentialsPath, credentialsJson);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempCredentialsPath);
    Console.WriteLine($"Firebase credentials loaded from environment variable and written to {tempCredentialsPath}");

    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(tempCredentialsPath)
    });

    var credential = GoogleCredential.FromFile(tempCredentialsPath);
    var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? builder.Configuration["GoogleCloud:ProjectId"];
    if (string.IsNullOrEmpty(projectId))
    {
        throw new InvalidOperationException("Google Cloud project ID not set.");
    }
    builder.Services.AddSingleton(FirestoreDb.Create(projectId, new FirestoreClientBuilder
    {
        Credential = credential
    }.Build()));

    // Configure logging
    builder.Logging.ClearProviders();
    builder.Logging.AddProvider(new CustomGoogleLoggerProvider(projectId));
    builder.Logging.AddConsole();
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing Firebase: {ex.Message}");
    throw;
}
finally
{
    if (File.Exists(tempCredentialsPath))
        File.Delete(tempCredentialsPath);
}

// Build the application
var app = builder.Build();

// Get logger after app is built
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Firebase setup completed with project ID: {ProjectId}", projectId);

// CORS for Vue
app.UseCors(options =>
{
    options.AddPolicy("AllowLocalDomain", policy =>
    {
        policy.WithOrigins("http://localhost:8080",
            "http://localhost",
            "http://localhost:8081",
            "http://localhost:9000",
            "http://localhost:9001",
            "http://family-budget.local:8081",
            "https://budget-buddy-a6b6c.web.app",
            "https://app.steadyrise.us",
            "https://budget-buddy-a6b6c.firebaseapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromDays(1));
    });
});

// Add controllers
app.MapControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimestampJsonConverter());
    });

// Add services
app.Services.AddSingleton<FamilyService>();
app.Services.AddSingleton<BudgetService>();
app.Services.AddSingleton<UserService>();
app.Services.AddSingleton<AccountService>();
app.Services.AddSingleton<BrevoService>();
app.Services.AddSingleton<StatementService>();

// Add Brevo
app.Services.Configure<FamilyBudgetApi.Models.BrevoSettings>(app.Configuration.GetSection("Brevo"));

// Configure Swagger
app.Services.AddEndpointsApiExplorer();
app.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "family-budget-api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer prefix (e.g., 'Bearer <token>')",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url = $"http://0.0.0.0:{port}";
logger.LogInformation("Starting server on {Url}", url);
app.Run(url);
