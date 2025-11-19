using MQTTnet;
using MQTTnet.Client;
using Spectre.Console;
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
    private static string _playlistTopic = "spotidial/playlists";
    private static string _albumTopic = "spotidial/albums";

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

        var getPlaylistsCommand = new Command("get-playlists", "Request list of user's playlists");

        var getAlbumsCommand = new Command("get-albums", "Request list of user's saved albums");

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
        rootCommand.AddCommand(getPlaylistsCommand);
        rootCommand.AddCommand(getAlbumsCommand);
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

        getPlaylistsCommand.SetHandler(async (host, port, username, password) =>
        {
            await GetPlaylistsAndDisplay(host, port, username, password);
        }, hostOption, portOption, usernameOption, passwordOption);

        getAlbumsCommand.SetHandler(async (host, port, username, password) =>
        {
            await GetAlbumsAndDisplay(host, port, username, password);
        }, hostOption, portOption, usernameOption, passwordOption);

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

    private static async Task GetPlaylistsAndDisplay(string host, int port, string username, string password)
    {
        _brokerHost = host;
        _brokerPort = port;
        _username = username;
        _password = password;

        var rule = new Rule("[bold cyan]SpotiDial Playlist Viewer[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"[dim]Connecting to {host}:{port}...[/]");

        try
        {
            await ConnectToMqtt();

            var playlistsReceived = new TaskCompletionSource<List<PlaylistData>>();

            // Subscribe to playlist topic
            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_playlistTopic)
                .Build());

            AnsiConsole.MarkupLine("[green]✓[/] Subscribed to playlist topic");

            // Set up message handler
            if (_mqttClient == null) return;

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                if (e.ApplicationMessage.Topic == _playlistTopic)
                {
                    try
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                        var playlists = JsonSerializer.Deserialize<List<PlaylistData>>(payload, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (playlists != null)
                        {
                            playlistsReceived.TrySetResult(playlists);
                        }
                    }
                    catch (Exception ex)
                    {
                        playlistsReceived.TrySetException(ex);
                    }
                }
                return Task.CompletedTask;
            };

            // Send the get_playlists command
            var payload = new { command = "get_playlists", parameter = (string?)null };
            var json = JsonSerializer.Serialize(payload);
            await PublishMessage(_commandTopic, json);

            AnsiConsole.MarkupLine("[green]✓[/] Command sent, waiting for response...");

            // Wait for response with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(playlistsReceived.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                AnsiConsole.MarkupLine("[red]✗[/] Timeout waiting for playlist data");
                await DisconnectFromMqtt();
                return;
            }

            var playlists = await playlistsReceived.Task;

            await DisconnectFromMqtt();

            // Display playlists as a table
            if (playlists == null || playlists.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]![/] No playlists found");
                return;
            }

            AnsiConsole.WriteLine();
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Owner[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Tracks[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Public[/]").Centered());
            table.AddColumn(new TableColumn("[bold]ID[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Description[/]").LeftAligned());

            foreach (var playlist in playlists)
            {
                var description = string.IsNullOrEmpty(playlist.Description)
                    ? "[dim]No description[/]"
                    : playlist.Description.Length > 50
                        ? playlist.Description.Substring(0, 47) + "..."
                        : playlist.Description;

                table.AddRow(
                    $"[cyan]{playlist.Name}[/]",
                    playlist.Owner ?? "[dim]Unknown[/]",
                    playlist.TrackCount.ToString(),
                    playlist.IsPublic ? "[green]Yes[/]" : "[red]No[/]",
                    $"[dim]{playlist.Id}[/]",
                    description
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Total playlists: {playlists.Count}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
        }
    }

    private static async Task GetAlbumsAndDisplay(string host, int port, string username, string password)
    {
        _brokerHost = host;
        _brokerPort = port;
        _username = username;
        _password = password;

        var rule = new Rule("[bold cyan]SpotiDial Album Viewer[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.MarkupLine($"[dim]Connecting to {host}:{port}...[/]");

        try
        {
            await ConnectToMqtt();

            var albumsReceived = new TaskCompletionSource<List<AlbumData>>();

            // Subscribe to album topic
            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_albumTopic)
                .Build());

            AnsiConsole.MarkupLine("[green]✓[/] Subscribed to album topic");

            // Set up message handler
            if (_mqttClient == null) return;

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                if (e.ApplicationMessage.Topic == _albumTopic)
                {
                    try
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                        var albums = JsonSerializer.Deserialize<List<AlbumData>>(payload, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (albums != null)
                        {
                            albumsReceived.TrySetResult(albums);
                        }
                    }
                    catch (Exception ex)
                    {
                        albumsReceived.TrySetException(ex);
                    }
                }
                return Task.CompletedTask;
            };

            // Send the get_albums command
            var payload = new { command = "get_albums", parameter = (string?)null };
            var json = JsonSerializer.Serialize(payload);
            await PublishMessage(_commandTopic, json);

            AnsiConsole.MarkupLine("[green]✓[/] Command sent, waiting for response...");

            // Wait for response with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(albumsReceived.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                AnsiConsole.MarkupLine("[red]✗[/] Timeout waiting for album data");
                await DisconnectFromMqtt();
                return;
            }

            var albums = await albumsReceived.Task;

            await DisconnectFromMqtt();

            // Display albums as a table
            if (albums == null || albums.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]![/] No albums found");
                return;
            }

            AnsiConsole.WriteLine();
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Artist[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Tracks[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Type[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Release Date[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]ID[/]").LeftAligned());

            foreach (var album in albums)
            {
                table.AddRow(
                    $"[cyan]{album.Name}[/]",
                    album.Artist ?? "[dim]Unknown[/]",
                    album.TrackCount.ToString(),
                    album.AlbumType ?? "[dim]N/A[/]",
                    album.ReleaseDate ?? "[dim]Unknown[/]",
                    $"[dim]{album.Id}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Total albums: {albums.Count}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
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
        Console.WriteLine("Monitoring status, image, playlist, and album updates...");
        Console.WriteLine("Press Ctrl+C to stop");
        Console.WriteLine();

        try
        {
            await ConnectToMqtt();

            // Subscribe to status, image, playlist, and album topics
            await _mqttClient!.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_statusTopic)
                .Build());

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_imageTopic)
                .Build());

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_playlistTopic)
                .Build());

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_albumTopic)
                .Build());

            Console.WriteLine($"✓ Subscribed to {_statusTopic}");
            Console.WriteLine($"✓ Subscribed to {_imageTopic}");
            Console.WriteLine($"✓ Subscribed to {_playlistTopic}");
            Console.WriteLine($"✓ Subscribed to {_albumTopic}");
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
                else if (topic == _playlistTopic)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] PLAYLIST UPDATE:");

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
                else if (topic == _albumTopic)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ALBUM UPDATE:");

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

public class PlaylistData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TrackCount { get; set; }
    public string? ImageUrl { get; set; }
    public string Owner { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string Uri { get; set; } = string.Empty;
}

public class AlbumData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int TrackCount { get; set; }
    public string? ImageUrl { get; set; }
    public string ReleaseDate { get; set; } = string.Empty;
    public string AlbumType { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
}
