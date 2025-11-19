using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotiDialBackend.Models;

namespace SpotiDialBackend.Services;

public class SpotifyService
{
    private readonly ILogger<SpotifyService> _logger;
    private readonly SpotifySettings _settings;
    private SpotifyClient? _spotify;
    private string? _currentTrackId;

    public event Action<SongInfo>? OnSongChanged;

    public SpotifyService(ILogger<SpotifyService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.Spotify;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Spotify client...");

            var config = SpotifyClientConfig.CreateDefault();
            var request = new AuthorizationCodeRefreshRequest(
                _settings.ClientId,
                _settings.ClientSecret,
                _settings.RefreshToken
            );

            var response = await new OAuthClient(config).RequestToken(request);
            _spotify = new SpotifyClient(config.WithToken(response.AccessToken));

            _logger.LogInformation("Spotify client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Spotify client");
            throw;
        }
    }

    public async Task<SongInfo?> GetCurrentSongInfoAsync()
    {
        if (_spotify == null) return null;

        try
        {
            var playback = await _spotify.Player.GetCurrentPlayback();
            if (playback?.Item is FullTrack track)
            {
                return new SongInfo
                {
                    TrackName = track.Name,
                    ArtistName = string.Join(", ", track.Artists.Select(a => a.Name)),
                    AlbumName = track.Album.Name,
                    DurationMs = track.DurationMs,
                    ProgressMs = playback.ProgressMs,
                    IsPlaying = playback.IsPlaying,
                    VolumePercent = playback.Device?.VolumePercent ?? 0,
                    AlbumImageUrl = track.Album.Images.FirstOrDefault()?.Url
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current song info");
        }

        return null;
    }

    public async Task MonitorPlaybackAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting playback monitoring...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var songInfo = await GetCurrentSongInfoAsync();
                if (songInfo != null && _spotify != null)
                {
                    var playback = await _spotify.Player.GetCurrentPlayback();
                    if (playback?.Item is FullTrack track)
                    {
                        if (_currentTrackId != track.Id)
                        {
                            _currentTrackId = track.Id;
                            _logger.LogInformation("Song changed: {Artist} - {Track}", songInfo.ArtistName, songInfo.TrackName);
                            OnSongChanged?.Invoke(songInfo);
                        }
                    }
                }

                await Task.Delay(_settings.PollingIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring playback");
                await Task.Delay(5000, cancellationToken);
            }
        }

        _logger.LogInformation("Playback monitoring stopped");
    }

    public async Task PlayAsync()
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.ResumePlayback();
            _logger.LogInformation("Playback resumed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming playback");
        }
    }

    public async Task PauseAsync()
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.PausePlayback();
            _logger.LogInformation("Playback paused");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing playback");
        }
    }

    public async Task NextTrackAsync()
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.SkipNext();
            _logger.LogInformation("Skipped to next track");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error skipping to next track");
        }
    }

    public async Task PreviousTrackAsync()
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.SkipPrevious();
            _logger.LogInformation("Skipped to previous track");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error skipping to previous track");
        }
    }

    public async Task SetVolumeAsync(int volumePercent)
    {
        if (_spotify == null) return;
        try
        {
            volumePercent = Math.Clamp(volumePercent, 0, 100);
            await _spotify.Player.SetVolume(new PlayerVolumeRequest(volumePercent));
            _logger.LogInformation("Volume set to {Volume}%", volumePercent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting volume");
        }
    }

    public async Task ChangeVolumeAsync(int delta)
    {
        if (_spotify == null) return;
        try
        {
            var playback = await _spotify.Player.GetCurrentPlayback();
            if (playback?.Device != null)
            {
                var currentVolume = playback.Device.VolumePercent ?? 50;
                var newVolume = Math.Clamp(currentVolume + delta, 0, 100);
                await SetVolumeAsync(newVolume);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing volume");
        }
    }

    public async Task ChangePlaylistAsync(string playlistId)
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest
            {
                ContextUri = $"spotify:playlist:{playlistId}"
            });
            _logger.LogInformation("Changed to playlist: {PlaylistId}", playlistId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing playlist");
        }
    }

    public async Task ChangeAlbumAsync(string albumId)
    {
        if (_spotify == null) return;
        try
        {
            await _spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest
            {
                ContextUri = $"spotify:album:{albumId}"
            });
            _logger.LogInformation("Changed to album: {AlbumId}", albumId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing album");
        }
    }

    public async Task<byte[]?> GetAlbumImageAsync(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;

        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetByteArrayAsync(imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading album image");
            return null;
        }
    }
}
