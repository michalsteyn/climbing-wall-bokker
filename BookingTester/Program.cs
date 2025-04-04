using BookingTester.Client;
using BookingTester.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookingTester.Services;

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
            options.DefaultBookingResult = BookStatus.AlreadyBooked;
            options.ServerTimeOffset = TimeSpan.Zero;
        });

        // Use mock implementation
        services.AddSingleton<IClimbingBooker, MockClimbingBooker>();

        //services.AddSingleton<IClimbingBooker, ClimbingBookerClient>();
        services.AddSingleton<IUserManager, UserManager>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddHostedService<BookingWorker>();
    });

var host = builder.Build();
await host.RunAsync();