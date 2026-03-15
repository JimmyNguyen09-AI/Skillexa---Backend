using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using skillexa_backend.Features.Auth;
using skillexa_backend.Features.Comments;
using skillexa_backend.Features.Courses;
using skillexa_backend.Features.Lessons;
using skillexa_backend.Features.Quizzes;
using skillexa_backend.Features.Stats;
using skillexa_backend.Features.Users;
using skillexa_backend.Infrastructure.Data;
using skillexa_backend.Infrastructure.Middleware;
using skillexa_backend.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("Jwt:Secret is required. Set it in appsettings or environment variables.");
}

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes for HS256. Use a longer random secret.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT bearer token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    var bearerReference = new OpenApiSecuritySchemeReference("Bearer", null, null);

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Skillexa API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            bearerReference,
            []
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var origins = GetAllowedOrigins(builder.Configuration);
        if (origins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.Name = "skillexa_external";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IStatsService, StatsService>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("DefaultCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await AppDbSeeder.SeedAdminAsync(dbContext, scope.ServiceProvider.GetRequiredService<IConfiguration>());
}

app.MapGet("/", () => Results.Ok(new { name = "Skillexa API", status = "running" })).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow })).AllowAnonymous();
app.MapGet("/health/db", async (AppDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect ? Results.Ok(new { status = "ok", database = "connected" }) : Results.Problem("Cannot connect to database");
}).AllowAnonymous();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapCourseEndpoints();
app.MapLessonEndpoints();
app.MapQuizEndpoints();
app.MapCommentEndpoints();
app.MapStatsEndpoints();

app.Run();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var csvOrigins = configuration["Cors:AllowedOriginsCsv"];
    if (!string.IsNullOrWhiteSpace(csvOrigins))
    {
        return csvOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    return configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
}
