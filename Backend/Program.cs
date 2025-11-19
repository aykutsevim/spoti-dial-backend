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
        // Load .env file
        DotNetEnv.Env.Load();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();

                // Map flat environment variables to hierarchical configuration
                var envVarMappings = new Dictionary<string, string>
                {
                    { "AppSettings:Mqtt:BrokerHost", Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? "localhost" },
                    { "AppSettings:Mqtt:BrokerPort", Environment.GetEnvironmentVariable("MQTT_BROKER_PORT") ?? "1883" },
                    { "AppSettings:Mqtt:ClientId", Environment.GetEnvironmentVariable("MQTT_CLIENT_ID") ?? "SpotiDialBackend" },
                    { "AppSettings:Mqtt:Username", Environment.GetEnvironmentVariable("MQTT_USERNAME") ?? "" },
                    { "AppSettings:Mqtt:Password", Environment.GetEnvironmentVariable("MQTT_PASSWORD") ?? "" },
                    { "AppSettings:Mqtt:CommandTopic", Environment.GetEnvironmentVariable("MQTT_COMMAND_TOPIC") ?? "spotidial/commands" },
                    { "AppSettings:Mqtt:StatusTopic", Environment.GetEnvironmentVariable("MQTT_STATUS_TOPIC") ?? "spotidial/status" },
                    { "AppSettings:Mqtt:ImageTopic", Environment.GetEnvironmentVariable("MQTT_IMAGE_TOPIC") ?? "spotidial/image" },
                    { "AppSettings:Spotify:ClientId", Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID") ?? "" },
                    { "AppSettings:Spotify:ClientSecret", Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET") ?? "" },
                    { "AppSettings:Spotify:RefreshToken", Environment.GetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN") ?? "" },
                    { "AppSettings:Spotify:PollingIntervalMs", Environment.GetEnvironmentVariable("SPOTIFY_POLLING_INTERVAL_MS") ?? "1000" }
                };

                config.AddInMemoryCollection(envVarMappings!);
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
