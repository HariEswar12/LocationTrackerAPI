using LocationTrackerAPI.Hubs;
using LocationTrackerAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 🔹 PORT CONFIG (Render)
// =============================
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// =============================
// 🔹 SERVICES
// =============================

// Controllers
builder.Services.AddControllers();

// Mongo + JWT
builder.Services.AddSingleton<MongoService>();
builder.Services.AddSingleton<JwtService>();

// SignalR
builder.Services.AddSignalR();

// CORS (React + SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins(
                "http://localhost:3000",               // local
                "https://your-app.vercel.app"          // ⚠️ replace later
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});


// =============================
// 🔐 JWT AUTHENTICATION
// =============================

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key missing");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = false,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // 🔥 SignalR JWT support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/locationHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();


// =============================
// 🔹 BUILD APP
// =============================

var app = builder.Build();


// =============================
// 🔹 MIDDLEWARE
// =============================

app.UseCors("AllowReact");

// app.UseHttpsRedirection(); // optional

app.UseAuthentication();
app.UseAuthorization();


// =============================
// 🔹 ENDPOINTS
// =============================

app.MapControllers();
app.MapHub<LocationHub>("/locationHub");

app.Run();