using BookingTester.Client;
using BookingTester.Models;
using BookingTester.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(configure => configure.AddConsole());
        
        // Configure event selection options
        services.Configure<EventSelectionOptions>(options =>
        {
            options.TargetTime = TimeSpan.FromHours(18);
            options.DaysAhead = 1;
        });

        // Configure mock options
        services.Configure<MockClimbingBookerOptions>(options =>
        {
            options.EventsFilePath = "events.json";
            options.DefaultBookingResult = BookStatus.OK;
            options.ServerTimeOffset = TimeSpan.Zero;
        });

        // Configure Hangfire
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
        //    .UseSQLiteStorage("hangfire.db;")
        );

        services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromSeconds(1));

        // Use mock implementation
        services.AddSingleton<IClimbingBooker, MockClimbingBooker>();
        services.AddSingleton<IUserManager, UserManager>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<IBookingScheduler, BookingScheduler>();
        services.AddHostedService<BookingWorker>();
    });

var host = builder.Build();

// Initialize Hangfire
//GlobalConfiguration.Configuration.UseSQLiteStorage("Data Source=hangfire.db;");

await host.RunAsync();