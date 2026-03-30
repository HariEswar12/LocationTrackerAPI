using LocationTrackerAPI.Hubs;
using LocationTrackerAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReact",
//        policy => policy
//            .WithOrigins("http://localhost:3000")
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials());
//});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy => policy
            .WithOrigins(
                "http://localhost:3000",               // local
                "https://your-app.vercel.app"          // production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// =============================
// 🔐 JWT AUTHENTICATION
// =============================

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // dev only
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = false,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // 🔥 no delay in token expiry
    };

    // 🔥 CRITICAL FOR SIGNALR (JWT via query string)
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

// Swagger / OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// =============================
// 🔹 MIDDLEWARE PIPELINE
// =============================

// CORS FIRST
app.UseCors("AllowReact");

// Disable HTTPS in dev (SignalR + localhost issues)
// app.UseHttpsRedirection();

// Auth (ORDER IS IMPORTANT)
app.UseAuthentication();
app.UseAuthorization();

// =============================
// 🔹 ENDPOINTS
// =============================

app.MapControllers();
app.MapHub<LocationHub>("/locationHub");

// Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();