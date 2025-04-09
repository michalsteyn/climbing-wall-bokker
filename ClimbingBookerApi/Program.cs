using BookingTester.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using ClimbingBookerApi.Filters;
using BookingTester.Client;
using ClimbingBookerApi.Middleware;

namespace ClimbingBookerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Climbing Booker API", Version = "v1" });
                
                // Add API key authentication to Swagger
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "API Key authentication",
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKey"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://8a452b6a-6b31-4857-96b1-db23ad2f812e.lovableproject.com", "https://id-preview--8a452b6a-6b31-4857-96b1-db23ad2f812e.lovable.app")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Configure Hangfire
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage("hangfire.db"));

            builder.Services.AddHangfireServer(options => 
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(10);
            });

            // Register services
            builder.Services.AddSingleton<IClimbingBooker, ClimbingBookerClient>();
            //builder.Services.AddSingleton<IClimbingBooker, MockClimbingBooker>();

            builder.Services.AddSingleton<IBookingService, BookingService>();
            builder.Services.AddSingleton<IBookingScheduler, BookingScheduler>();
            builder.Services.AddSingleton<IEventManager, EventManager>();
            builder.Services.AddSingleton<IUserManager, UserManager>();

            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Climbing Booker API v1");
                });
            }

            //app.UseHttpsRedirection();

            // Enable CORS
            app.UseCors("AllowFrontend");

            // Add API key middleware before authorization
            app.UseApiKeyMiddleware();

            app.UseAuthorization();
            app.MapControllers();

            // Configure Hangfire dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            app.Run();
        }
    }
}
