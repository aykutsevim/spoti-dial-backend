using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SpotiDialBackend.Models;

namespace SpotiDialBackend.Services;

public class MqttService
{
    private readonly ILogger<MqttService> _logger;
    private readonly MqttSettings _settings;
    private IManagedMqttClient? _mqttClient;

    public event Action<DeviceCommand>? OnCommandReceived;

    public MqttService(ILogger<MqttService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.Mqtt;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to MQTT broker at {Host}:{Port}...",
                _settings.BrokerHost, _settings.BrokerPort);

            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.BrokerHost, _settings.BrokerPort)
                .WithClientId(_settings.ClientId);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                mqttClientOptions.WithCredentials(_settings.Username, _settings.Password);
            }

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(mqttClientOptions.Build())
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

            await _mqttClient.StartAsync(managedOptions);

            _logger.LogInformation("MQTT client started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker");
            throw;
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        _logger.LogInformation("Connected to MQTT broker");

        try
        {
            await _mqttClient!.SubscribeAsync(_settings.CommandTopic);
            _logger.LogInformation("Subscribed to topic: {Topic}", _settings.CommandTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to topics");
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Reason: {Reason}", args.Reason);
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            _logger.LogDebug("Received message on topic {Topic}: {Payload}",
                args.ApplicationMessage.Topic, payload);

            if (args.ApplicationMessage.Topic == _settings.CommandTopic)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var command = JsonSerializer.Deserialize<DeviceCommand>(payload, options);
                if (command != null)
                {
                    _logger.LogInformation("Command received: {Command} {Parameter}",
                        command.Command, command.Parameter ?? "");
                    OnCommandReceived?.Invoke(command);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }

        return Task.CompletedTask;
    }

    public async Task PublishSongInfoAsync(SongInfo songInfo)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        try
        {
            var payload = JsonSerializer.Serialize(songInfo);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_settings.StatusTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logger.LogDebug("Published song info to {Topic}", _settings.StatusTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing song info");
        }
    }

    public async Task PublishImageAsync(byte[] imageData)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_settings.ImageTopic)
                .WithPayload(imageData)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logger.LogInformation("Published image to {Topic} ({Size} bytes)",
                _settings.ImageTopic, imageData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing image");
        }
    }

    public async Task PublishPlaylistsAsync(List<PlaylistInfo> playlists)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        try
        {
            var payload = JsonSerializer.Serialize(playlists);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_settings.PlaylistTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logger.LogInformation("Published {Count} playlists to {Topic}", playlists.Count, _settings.PlaylistTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing playlists");
        }
    }

    public async Task PublishAlbumsAsync(List<AlbumInfo> albums)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected) return;

        try
        {
            var payload = JsonSerializer.Serialize(albums);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_settings.AlbumTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logger.LogInformation("Published {Count} albums to {Topic}", albums.Count, _settings.AlbumTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing albums");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient != null)
        {
            _logger.LogInformation("Disconnecting from MQTT broker...");
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
            _logger.LogInformation("Disconnected from MQTT broker");
        }
    }
}
