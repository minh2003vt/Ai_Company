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
using Application.Helper;
using Ai_Company.ActionLogging;
using Application.Options;
using Application.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Cấu hình JSON serializer để bỏ qua các property không thể serialize (như Exception.TargetSite)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // Bỏ qua các property không thể serialize
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // Custom converter để xử lý Exception objects
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
// Configure GeminiOptions - IOptionsSnapshot will reload on each request
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));

// 🔹 CORS Configuration - THÊM VÀO ĐÂY
builder.Services.AddCors(options =>
{
    options.AddPolicy("GoogleOAuthCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:8081",
                "http://localhost:8082",
                "http://localhost:8083",
                "https://localhost:52393",
                "https://localhost:8080",
                "https://localhost:8081",
                "https://localhost:8082",
                "https://localhost:8083"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains();
    });

    // Development policy - cho phép tất cả origins
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.AllowAnyOrigin()
             .AllowAnyHeader()
             .AllowAnyMethod();
    });
});

// DbContext - Build connection string from environment variables if not set
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Build from individual environment variables (Render/Docker compatible)
    // ASP.NET Core automatically maps env vars: POSTGRES_HOST -> Configuration["POSTGRES_HOST"]
    var dbHost = builder.Configuration["POSTGRES_HOST"];
    var dbPort = builder.Configuration["POSTGRES_PORT"] ?? "5432";
    var dbName = builder.Configuration["POSTGRES_DB"];
    var dbUser = builder.Configuration["POSTGRES_USER"];
    var dbPassword = builder.Configuration["POSTGRES_PASSWORD"];
    
    if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbName))
    {
        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        throw new InvalidOperationException("Database connection string is not configured. Please set ConnectionStrings:DefaultConnection or individual POSTGRES_* environment variables.");
    }
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

    // Resolve đường dẫn: nếu là đường dẫn tương đối, tìm từ thư mục gốc của ứng dụng
    string resolvedPath = firebaseKeyPath;
    if (!Path.IsPathRooted(firebaseKeyPath))
    {
        // Đường dẫn tương đối - tìm từ thư mục gốc của ứng dụng
        var baseDirectory = AppContext.BaseDirectory;
        resolvedPath = Path.Combine(baseDirectory, firebaseKeyPath);
        
        // Nếu không tìm thấy, thử từ thư mục hiện tại
        if (!File.Exists(resolvedPath))
        {
            resolvedPath = Path.Combine(Directory.GetCurrentDirectory(), firebaseKeyPath);
        }
    }

    if (!File.Exists(resolvedPath))
        throw new FileNotFoundException($"Firebase credential file not found: {resolvedPath}");

    // Load Google credentials from file
    GoogleCredential credential = GoogleCredential.FromFile(resolvedPath);

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
builder.Services.AddHttpClient<Application.Service.ChatService>();
builder.Services.AddScoped<Application.Service.QdrantService>();
builder.Services.AddScoped<QdrantHelper>();

// Repositories DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ILoginLogsRepository, LoginLogsRepository>();
builder.Services.AddScoped<IActionLogRepository, ActionLogRepository>();
builder.Services.AddScoped<IUserDepartmentRepository, UserDepartmentRepository>();
builder.Services.AddScoped<IUserAiConfigRepository, UserAiConfigRepository>();
builder.Services.AddScoped<IAIConfigureRepository, AIConfigureRepository>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IKnowledgeSourceRepository, KnowledgeSourceRepository>();
builder.Services.AddScoped<IAIConfigureVersionRepository, AIConfigureVersionRepository>();
builder.Services.AddScoped<IAIConfigureCompanyRepository, AIConfigureCompanyRepository>();
builder.Services.AddScoped<IAIConfigureCompanyDepartmentRepository, AIConfigureCompanyDepartmentRepository>();
builder.Services.AddScoped<IAIModelConfigRepository, AIModelConfigRepository>();
// Removed Document repository as Document entity was dropped
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));
});
// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAIConfigureService, AIConfigureService>();
builder.Services.AddScoped<IUserAiConfigService, UserAiConfigService>();
builder.Services.AddScoped<IChatService, Application.Service.ChatService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserDepartmentService, UserDepartmentService>();
builder.Services.AddScoped<IActionLogService, ActionLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAIConfigureCompanyService, AIConfigureCompanyService>();
builder.Services.AddScoped<IAIConfigureCompanyDepartmentService, AIConfigureCompanyDepartmentService>();
builder.Services.AddScoped<IKnowledgeSourceService, KnowledgeSourceService>();
builder.Services.AddScoped<IAIModelConfigService, AIModelConfigService>();

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

// 🔹 MIDDLEWARE PIPELINE - THỨ TỰ QUAN TRỌNG
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 CORS MIDDLEWARE - PHẢI ĐẶT TRƯỚC Authentication
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("GoogleOAuthCors");
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();

// Global action logging after auth
app.UseMiddleware<ActionLoggingMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

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
