using BookingTester.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using ClimbingBookerApi.Filters;
using BookingTester.Client;

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
            });

            // Configure Hangfire
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage("hangfire.db"));

            builder.Services.AddHangfireServer();

            // Register services
            //builder.Services.AddSingleton<IClimbingBooker, ClimbingBookerApiService>();
            builder.Services.AddSingleton<IClimbingBooker, MockClimbingBooker>();

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
