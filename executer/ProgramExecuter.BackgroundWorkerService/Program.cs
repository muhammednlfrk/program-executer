using ProgramExecuter.BackgroundWorkerService;
using ProgramExecuter.BackgroundWorkerService.Mail;

// Create builder
IHostBuilder builder = Host.CreateDefaultBuilder();

IConfiguration configuration = null;

// Configure services
builder.ConfigureServices((context, services) =>
{
    // Get configuration
    configuration = context.Configuration;

    // Add program services
    services.AddSingleton<ProgramExecuterService>();
    services.AddSingleton<IMailService, SmtpMailService>();

    // Add logging
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConsole();
    });

    // Add workers
    services.AddHostedService<ProgramWeorkerbackgroundService>();
});

// Windows Service options
builder.UseWindowsService(options =>
{
    options.ServiceName = configuration["WorkerOptions:ServiceName"];
});

// Build host
IHost host = builder.Build();

// Run service worker
await host.RunAsync();
