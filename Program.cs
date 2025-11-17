using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotiDialBackend.Models;
using SpotiDialBackend.Services;

namespace SpotiDialBackend;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

                // Services
                services.AddSingleton<MqttService>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<ImageProcessingService>();

                // Background service
                services.AddHostedService<CommandProcessorService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        Console.WriteLine("===========================================");
        Console.WriteLine("   Spoti-Dial Backend");
        Console.WriteLine("   ESP32 M5Dial <-> Spotify Bridge");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
