using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotiDialBackend.Models;

namespace SpotiDialBackend.Services;

public class CommandProcessorService : BackgroundService
{
    private readonly ILogger<CommandProcessorService> _logger;
    private readonly MqttService _mqttService;
    private readonly SpotifyService _spotifyService;
    private readonly ImageProcessingService _imageService;

    public CommandProcessorService(
        ILogger<CommandProcessorService> logger,
        MqttService mqttService,
        SpotifyService spotifyService,
        ImageProcessingService imageService)
    {
        _logger = logger;
        _mqttService = mqttService;
        _spotifyService = spotifyService;
        _imageService = imageService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Command Processor Service...");

            // Initialize services
            await _spotifyService.InitializeAsync();
            await _mqttService.ConnectAsync();

            // Subscribe to events
            _mqttService.OnCommandReceived += async (command) => await HandleCommandAsync(command);
            _spotifyService.OnSongChanged += async (songInfo) => await HandleSongChangedAsync(songInfo);

            // Start monitoring Spotify playback
            _logger.LogInformation("Command Processor Service started");
            await _spotifyService.MonitorPlaybackAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Command Processor Service");
            throw;
        }
    }

    private async Task HandleCommandAsync(DeviceCommand command)
    {
        _logger.LogInformation("Processing command: {Command}", command.Command);

        try
        {
            switch (command.Command.ToLowerInvariant())
            {
                case Commands.Play:
                    await _spotifyService.PlayAsync();
                    break;

                case Commands.Pause:
                    await _spotifyService.PauseAsync();
                    break;

                case Commands.NextTrack:
                    await _spotifyService.NextTrackAsync();
                    break;

                case Commands.PreviousTrack:
                    await _spotifyService.PreviousTrackAsync();
                    break;

                case Commands.VolumeUp:
                    await _spotifyService.ChangeVolumeAsync(5);
                    break;

                case Commands.VolumeDown:
                    await _spotifyService.ChangeVolumeAsync(-5);
                    break;

                case Commands.SetVolume:
                    if (int.TryParse(command.Parameter, out int volume))
                    {
                        await _spotifyService.SetVolumeAsync(volume);
                    }
                    break;

                case Commands.ChangePlaylist:
                    if (!string.IsNullOrEmpty(command.Parameter))
                    {
                        await _spotifyService.ChangePlaylistAsync(command.Parameter);
                    }
                    break;

                case Commands.ChangeAlbum:
                    if (!string.IsNullOrEmpty(command.Parameter))
                    {
                        await _spotifyService.ChangeAlbumAsync(command.Parameter);
                    }
                    break;

                case Commands.GetPlaylists:
                    var playlists = await _spotifyService.GetUserPlaylistsAsync();
                    await _mqttService.PublishPlaylistsAsync(playlists);
                    break;

                case Commands.GetAlbums:
                    var albums = await _spotifyService.GetUserAlbumsAsync();
                    await _mqttService.PublishAlbumsAsync(albums);
                    break;

                default:
                    _logger.LogWarning("Unknown command: {Command}", command.Command);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command: {Command}", command.Command);
        }
    }

    private async Task HandleSongChangedAsync(SongInfo songInfo)
    {
        _logger.LogInformation("Handling song change: {Artist} - {Track}",
            songInfo.ArtistName, songInfo.TrackName);

        try
        {
            // Publish song info to MQTT
            await _mqttService.PublishSongInfoAsync(songInfo);

            // Download and publish album artwork
            if (!string.IsNullOrEmpty(songInfo.AlbumImageUrl))
            {
                var imageData = await _spotifyService.GetAlbumImageAsync(songInfo.AlbumImageUrl);
                if (imageData != null)
                {
                    var processedImage = await _imageService.ProcessImageForDeviceAsync(imageData);
                    if (processedImage != null)
                    {
                        await _mqttService.PublishImageAsync(processedImage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling song change");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Command Processor Service...");
        await _mqttService.DisconnectAsync();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Command Processor Service stopped");
    }
}
