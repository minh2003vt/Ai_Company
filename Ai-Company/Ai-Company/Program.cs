using Infrastructure;
using Infrastructure.Repository;
using Infrastructure.Repository.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.Service.Interfaces;
using Application.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Ai_Company.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 🔹 Firebase Initialization
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var firebaseKeyPath = config["Firebase:CredentialPath"];
    var projectId = config["Firebase:ProjectId"];

    if (string.IsNullOrEmpty(firebaseKeyPath) || string.IsNullOrEmpty(projectId))
        throw new InvalidOperationException("Firebase configuration is missing!");

    // Load Google credentials from file
    GoogleCredential credential = GoogleCredential.FromFile(firebaseKeyPath);

    // Build FirestoreClient with those credentials
    var firestoreClient = new FirestoreClientBuilder
    {
        Credential = credential
    }.Build();

    // Return FirestoreDb with explicit client
    return FirestoreDb.Create(projectId, firestoreClient);
});

builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddHttpClient<DocumentService>();

// Repositories DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ILoginLogsRepository, LoginLogsRepository>();
builder.Services.AddScoped<IActionLogRepository, ActionLogRepository>();
builder.Services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
builder.Services.AddScoped<IUserDepartmentRepository, UserDepartmentRepository>();
builder.Services.AddScoped<IAIConfigureRepository, AIConfigureRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();

// 🔹 JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    Console.WriteLine("Jwt:Key not set. Set appsettings or env JWT__KEY before running in production.");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? "development-only-key-change-me"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = key
    };
});

// 🔹 Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ai-Company", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 🔹 Seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Database.CanConnectAsync())
        {
            await Infrastructure.DataSeeder.SeedAsync(db);
        }
        else
        {
            Console.WriteLine("Cannot connect to database. Skipping seeding.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed error: {ex.Message}");
    }
}

app.Run();
