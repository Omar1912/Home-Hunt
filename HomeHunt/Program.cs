using HomeHunt.Services.Interfaces;
using HomeHunt.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HomeHunt.Data;
using Microsoft.EntityFrameworkCore;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.Json;
using HomeHunt.Services.Filters;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure Entity Framework Core with SQL Server (single registration)
builder.Services.AddDbContext<HomeHuntDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RemoteConnectionString")));

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Configure ASP.NET Core Identity with UserEntity
builder.Services.AddIdentity<UserEntity, IdentityRole<int>>()
    .AddEntityFrameworkStores<HomeHuntDBContext>()
    .AddDefaultTokenProviders();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")?.Get<string[]>()
    ?? new[] { "http://localhost:5173" }; // Replace with production URL

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", corsBuilder =>
    {
        corsBuilder.WithOrigins(allowedOrigins)
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials(); // Remove if credentials aren't needed
    });
});

// Configure controllers with camelCase JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Add services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPropertyFilterService, PropertyFilterService>();
builder.Services.AddScoped<ITourRequestService, TourRequestService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IEmailService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var sendGridSection = config.GetSection("SendGrid");

    var apiKey = sendGridSection.GetValue<string>("ApiKey");
    var fromEmail = sendGridSection.GetValue<string>("FromEmail");
    var fromName = sendGridSection.GetValue<string>("FromName");

    if (string.IsNullOrEmpty(apiKey))
        throw new ArgumentNullException(nameof(apiKey), "SendGrid:ApiKey is missing in configuration.");

    return new EmailService(apiKey, fromEmail, fromName);
});


// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HomeHunt API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
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
            Array.Empty<string>()
        }
    });
});

 
// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Enforce HTTPS in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // strictly respect the 'exp' time
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
 
// Configure the HTTP request pipeline
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred. Please try again later.\"}");
    });
});

app.UseSwagger();
app.UseSwaggerUI();


//app.UseHttpsRedirection(); // Enable HTTPS in production
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();
app.Run();