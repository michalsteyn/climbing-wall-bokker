using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookingTester.Services;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<IClimbingBooker, ClimbingBookerClient>();
        services.AddSingleton<IUserManager, UserManager>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddHostedService<BookingWorker>();
    });

var host = builder.Build();
await host.RunAsync();