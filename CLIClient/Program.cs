using MQTTnet;
using MQTTnet.Client;
using System.CommandLine;
using System.Text;
using System.Text.Json;

namespace CLIClient;

class Program
{
    private static IMqttClient? _mqttClient;
    private static string _brokerHost = "localhost";
    private static int _brokerPort = 1883;
    private static string _username = "";
    private static string _password = "";
    private static string _commandTopic = "spotidial/commands";
    private static string _statusTopic = "spotidial/status";
    private static string _imageTopic = "spotidial/image";

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("SpotiDial MQTT CLI Client - Test tool for simulating M5Dial commands");

        // Global options
        var hostOption = new Option<string>(
            aliases: new[] { "--host", "-h" },
            getDefaultValue: () => "localhost",
            description: "MQTT broker host");

        var portOption = new Option<int>(
            aliases: new[] { "--port", "-p" },
            getDefaultValue: () => 1883,
            description: "MQTT broker port");

        var usernameOption = new Option<string>(
            aliases: new[] { "--username", "-u" },
            getDefaultValue: () => "",
            description: "MQTT username (optional)");

        var passwordOption = new Option<string>(
            aliases: new[] { "--password", "-pw" },
            getDefaultValue: () => "",
            description: "MQTT password (optional)");

        rootCommand.AddGlobalOption(hostOption);
        rootCommand.AddGlobalOption(portOption);
        rootCommand.AddGlobalOption(usernameOption);
        rootCommand.AddGlobalOption(passwordOption);

        // Commands
        var playCommand = new Command("play", "Resume playback");
        var pauseCommand = new Command("pause", "Pause playback");
        var nextCommand = new Command("next", "Skip to next track");
        var previousCommand = new Command("previous", "Skip to previous track");
        var volumeUpCommand = new Command("volume-up", "Increase volume by 5%");
        var volumeDownCommand = new Command("volume-down", "Decrease volume by 5%");

        var setVolumeCommand = new Command("set-volume", "Set volume to specific level");
        var volumeArgument = new Argument<int>("level", "Volume level (0-100)");
        setVolumeCommand.AddArgument(volumeArgument);

        var changePlaylistCommand = new Command("change-playlist", "Change to a playlist");
        var playlistArgument = new Argument<string>("playlist-id", "Spotify playlist ID");
        changePlaylistCommand.AddArgument(playlistArgument);

        var changeAlbumCommand = new Command("change-album", "Change to an album");
        var albumArgument = new Argument<string>("album-id", "Spotify album ID");
        changeAlbumCommand.AddArgument(albumArgument);

        var monitorCommand = new Command("monitor", "Monitor status and image updates from the backend");

        // Add commands to root
        rootCommand.AddCommand(playCommand);
        rootCommand.AddCommand(pauseCommand);
        rootCommand.AddCommand(nextCommand);
        rootCommand.AddCommand(previousCommand);
        rootCommand.AddCommand(volumeUpCommand);
        rootCommand.AddCommand(volumeDownCommand);
        rootCommand.AddCommand(setVolumeCommand);
        rootCommand.AddCommand(changePlaylistCommand);
        rootCommand.AddCommand(changeAlbumCommand);
        rootCommand.AddCommand(monitorCommand);

        // Set handlers
        playCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "play", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        pauseCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "pause", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        nextCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "next", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        previousCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "previous", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        volumeUpCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "volume_up", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        volumeDownCommand.SetHandler(async (host, port, username, password) =>
        {
            await ExecuteCommand(host, port, username, password, "volume_down", null);
        }, hostOption, portOption, usernameOption, passwordOption);

        setVolumeCommand.SetHandler(async (host, port, username, password, level) =>
        {
            await ExecuteCommand(host, port, username, password, "set_volume", level.ToString());
        }, hostOption, portOption, usernameOption, passwordOption, volumeArgument);

        changePlaylistCommand.SetHandler(async (host, port, username, password, playlistId) =>
        {
            await ExecuteCommand(host, port, username, password, "change_playlist", playlistId);
        }, hostOption, portOption, usernameOption, passwordOption, playlistArgument);

        changeAlbumCommand.SetHandler(async (host, port, username, password, albumId) =>
        {
            await ExecuteCommand(host, port, username, password, "change_album", albumId);
        }, hostOption, portOption, usernameOption, passwordOption, albumArgument);

        monitorCommand.SetHandler(async (host, port, username, password) =>
        {
            await MonitorUpdates(host, port, username, password);
        }, hostOption, portOption, usernameOption, passwordOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task ExecuteCommand(string host, int port, string username, string password, string command, string? parameter)
    {
        _brokerHost = host;
        _brokerPort = port;
        _username = username;
        _password = password;

        Console.WriteLine("=========================================");
        Console.WriteLine("  SpotiDial MQTT CLI Client");
        Console.WriteLine("=========================================");
        Console.WriteLine($"Connecting to {host}:{port}...");

        try
        {
            await ConnectToMqtt();

            var payload = new
            {
                command = command,
                parameter = parameter
            };

            var json = JsonSerializer.Serialize(payload);
            Console.WriteLine($"Sending command: {json}");

            await PublishMessage(_commandTopic, json);

            Console.WriteLine("✓ Command sent successfully!");

            await DisconnectFromMqtt();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    private static async Task MonitorUpdates(string host, int port, string username, string password)
    {
        _brokerHost = host;
        _brokerPort = port;
        _username = username;
        _password = password;

        Console.WriteLine("=========================================");
        Console.WriteLine("  SpotiDial MQTT Monitor");
        Console.WriteLine("=========================================");
        Console.WriteLine($"Connecting to {host}:{port}...");
        Console.WriteLine("Monitoring status and image updates...");
        Console.WriteLine("Press Ctrl+C to stop");
        Console.WriteLine();

        try
        {
            await ConnectToMqtt();

            // Subscribe to status and image topics
            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_statusTopic)
                .Build());

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_imageTopic)
                .Build());

            Console.WriteLine($"✓ Subscribed to {_statusTopic}");
            Console.WriteLine($"✓ Subscribed to {_imageTopic}");
            Console.WriteLine();

            // Set up message handler
            if (_mqttClient == null) return;

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;

                if (topic == _statusTopic)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] STATUS UPDATE:");

                    try
                    {
                        var json = JsonSerializer.Deserialize<JsonElement>(payload);
                        var formatted = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine(formatted);
                    }
                    catch
                    {
                        Console.WriteLine(payload);
                    }
                    Console.WriteLine();
                }
                else if (topic == _imageTopic)
                {
                    var size = e.ApplicationMessage.PayloadSegment.Count;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] IMAGE UPDATE: Received {size} bytes");
                    Console.WriteLine();
                }

                return Task.CompletedTask;
            };

            // Keep the application running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    private static async Task ConnectToMqtt()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_brokerHost, _brokerPort)
            .WithClientId($"CLIClient-{Guid.NewGuid()}")
            .WithCleanSession();

        if (!string.IsNullOrEmpty(_username))
        {
            optionsBuilder.WithCredentials(_username, _password);
        }

        var options = optionsBuilder.Build();

        await _mqttClient.ConnectAsync(options);
        Console.WriteLine("✓ Connected to MQTT broker");
    }

    private static async Task DisconnectFromMqtt()
    {
        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
            Console.WriteLine("✓ Disconnected from MQTT broker");
        }
    }

    private static async Task PublishMessage(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient!.PublishAsync(message);
    }
}
