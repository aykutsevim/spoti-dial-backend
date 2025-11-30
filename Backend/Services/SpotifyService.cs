using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotiDialBackend.Models;

namespace SpotiDialBackend.Services;

public class SpotifyService
{
    private readonly ILogger<SpotifyService> _logger;
    private readonly SpotifySettings _settings;
    private readonly TokenStorageService _tokenStorage;
    private SpotifyClient? _spotify;
    private string? _currentTrackId;
    private string? _refreshToken;

    public event Action<SongInfo>? OnSongChanged;

    public SpotifyService(
        ILogger<SpotifyService> logger,
        IOptions<AppSettings> settings,
        TokenStorageService tokenStorage)
    {
        _logger = logger;
        _settings = settings.Value.Spotify;
        _tokenStorage = tokenStorage;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Spotify client with automatic token refresh...");

            // Try to get refresh token from storage first
            _refreshToken = await _tokenStorage.GetRefreshTokenAsync();

            // If no stored token and no env token, initiate OAuth flow
            if (string.IsNullOrEmpty(_refreshToken) && string.IsNullOrEmpty(_settings.RefreshToken))
            {
                _logger.LogWarning("No refresh token found. Starting OAuth authorization flow...");
                _refreshToken = await PerformOAuthFlowAsync();

                if (string.IsNullOrEmpty(_refreshToken))
                {
                    throw new Exception("Failed to obtain refresh token from OAuth flow");
                }

                // Save the refresh token for future use
                await _tokenStorage.SaveRefreshTokenAsync(_refreshToken);
            }
            else if (!string.IsNullOrEmpty(_settings.RefreshToken))
            {
                // Use refresh token from environment if available
                _logger.LogInformation("Using refresh token from configuration");
                _refreshToken = _settings.RefreshToken;

                // Save it to storage for future use
                await _tokenStorage.SaveRefreshTokenAsync(_refreshToken);
            }

            // Create authenticator with automatic token refresh
            var authenticator = new AuthorizationCodeAuthenticator(
                _settings.ClientId,
                _settings.ClientSecret,
                new AuthorizationCodeRefreshResponse
                {
                    RefreshToken = _refreshToken,
                    AccessToken = "initial", // Will be refreshed immediately
                    ExpiresIn = 0 // Force immediate refresh
                }
            );

            // Set up token refresh callback
            authenticator.TokenRefreshed += (sender, response) =>
            {
                _logger.LogInformation("Spotify access token refreshed successfully");
            };

            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator);

            _spotify = new SpotifyClient(config);

            _logger.LogInformation("Spotify client initialized successfully with automatic token refresh");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Spotify client");
            throw;
        }
    }

    private async Task<string?> PerformOAuthFlowAsync()
    {
        var tcs = new TaskCompletionSource<string?>();
        EmbedIOAuthServer? server = null;

        try
        {
            _logger.LogInformation("Starting OAuth server on port {Port}...", _settings.OAuthCallbackPort);

            server = new EmbedIOAuthServer(
                new Uri(_settings.OAuthRedirectUri),
                _settings.OAuthCallbackPort
            );

            server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await server.Stop();

                try
                {
                    var tokenResponse = await new OAuthClient().RequestToken(
                        new AuthorizationCodeTokenRequest(
                            _settings.ClientId,
                            _settings.ClientSecret,
                            response.Code,
                            new Uri(_settings.OAuthRedirectUri)
                        )
                    );

                    _logger.LogInformation("OAuth authorization successful!");
                    tcs.TrySetResult(tokenResponse.RefreshToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exchanging authorization code for token");
                    tcs.TrySetResult(null);
                }
            };

            server.ErrorReceived += (sender, error, state) =>
            {
                _logger.LogError("OAuth authorization error: {Error}", error);
                tcs.TrySetResult(null);
                return Task.CompletedTask;
            };

            await server.Start();

            var loginRequest = new LoginRequest(
                new Uri(_settings.OAuthRedirectUri),
                _settings.ClientId,
                LoginRequest.ResponseType.Code
            )
            {
                Scope = new[]
                {
                    Scopes.UserReadPlaybackState,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.UserLibraryRead
                }
            };

            var uri = loginRequest.ToUri();

            _logger.LogWarning("========================================");
            _logger.LogWarning("SPOTIFY AUTHORIZATION REQUIRED");
            _logger.LogWarning("========================================");
            _logger.LogWarning("Please open the following URL in your browser to authorize:");
            _logger.LogWarning("{AuthUrl}", uri);
            _logger.LogWarning("========================================");
            _logger.LogWarning("IMPORTANT: Make sure the redirect URI is added to your Spotify app settings:");
            _logger.LogWarning("Go to: https://developer.spotify.com/dashboard");
            _logger.LogWarning("Edit your app > Settings > Redirect URIs > Add: {RedirectUri}", _settings.OAuthRedirectUri);
            _logger.LogWarning("========================================");

            // Try to open browser automatically
            try
            {
                BrowserUtil.Open(uri);
                _logger.LogInformation("Browser opened automatically for authorization");
            }
            catch
            {
                _logger.LogWarning("Could not open browser automatically. Please open the URL manually.");
            }

            // Wait for authorization with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogError("OAuth authorization timed out after 5 minutes");
                return null;
            }

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth flow");
            return null;
        }
        finally
        {
            if (server != null)
            {
                await server.Stop();
            }
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
        catch (SpotifyAPI.Web.APIException apiEx)
        {
            if (apiEx.Message.Contains("Restriction violated") || apiEx.Message.Contains("NO_ACTIVE_DEVICE"))
            {
                _logger.LogWarning("Cannot resume playback: No active Spotify device found or Spotify Premium required. Please start playback on a Spotify device first.");
            }
            else
            {
                _logger.LogError(apiEx, "Spotify API error while resuming playback");
            }
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

    public async Task<List<PlaylistInfo>> GetUserPlaylistsAsync()
    {
        if (_spotify == null) return new List<PlaylistInfo>();

        try
        {
            var playlists = new List<PlaylistInfo>();
            var currentPlaylists = await _spotify.Playlists.CurrentUsers();

            await foreach (var playlist in _spotify.Paginate(currentPlaylists))
            {
                playlists.Add(new PlaylistInfo
                {
                    Id = playlist.Id ?? string.Empty,
                    Name = playlist.Name ?? string.Empty,
                    Description = playlist.Description,
                    TrackCount = playlist.Tracks?.Total ?? 0,
                    ImageUrl = playlist.Images?.FirstOrDefault()?.Url,
                    Owner = playlist.Owner?.DisplayName ?? string.Empty,
                    IsPublic = playlist.Public ?? false,
                    Uri = playlist.Uri ?? string.Empty
                });
            }

            _logger.LogInformation("Retrieved {Count} playlists", playlists.Count);
            return playlists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user playlists");
            return new List<PlaylistInfo>();
        }
    }

    public async Task<PlaylistInfo?> GetPlaylistAsync(string playlistId)
    {
        if (_spotify == null) return null;

        try
        {
            var playlist = await _spotify.Playlists.Get(playlistId);
            if (playlist == null) return null;

            var playlistInfo = new PlaylistInfo
            {
                Id = playlist.Id ?? string.Empty,
                Name = playlist.Name ?? string.Empty,
                Description = playlist.Description,
                TrackCount = playlist.Tracks?.Total ?? 0,
                ImageUrl = playlist.Images?.FirstOrDefault()?.Url,
                Owner = playlist.Owner?.DisplayName ?? string.Empty,
                IsPublic = playlist.Public ?? false,
                Uri = playlist.Uri ?? string.Empty
            };

            _logger.LogInformation("Retrieved playlist: {PlaylistName}", playlistInfo.Name);
            return playlistInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playlist {PlaylistId}", playlistId);
            return null;
        }
    }

    public async Task<List<AlbumInfo>> GetUserAlbumsAsync()
    {
        if (_spotify == null) return new List<AlbumInfo>();

        try
        {
            var albums = new List<AlbumInfo>();
            var currentAlbums = await _spotify.Library.GetAlbums();

            await foreach (var savedAlbum in _spotify.Paginate(currentAlbums))
            {
                var album = savedAlbum.Album;
                albums.Add(new AlbumInfo
                {
                    Id = album.Id ?? string.Empty,
                    Name = album.Name ?? string.Empty,
                    Artist = string.Join(", ", album.Artists.Select(a => a.Name)),
                    TrackCount = album.TotalTracks,
                    ImageUrl = album.Images?.FirstOrDefault()?.Url,
                    ReleaseDate = album.ReleaseDate ?? string.Empty,
                    AlbumType = album.AlbumType ?? string.Empty,
                    Uri = album.Uri ?? string.Empty
                });
            }

            _logger.LogInformation("Retrieved {Count} albums", albums.Count);
            return albums;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user albums");
            return new List<AlbumInfo>();
        }
    }

    public async Task<AlbumInfo?> GetAlbumAsync(string albumId)
    {
        if (_spotify == null) return null;

        try
        {
            var album = await _spotify.Albums.Get(albumId);
            if (album == null) return null;

            var albumInfo = new AlbumInfo
            {
                Id = album.Id ?? string.Empty,
                Name = album.Name ?? string.Empty,
                Artist = string.Join(", ", album.Artists.Select(a => a.Name)),
                TrackCount = album.TotalTracks,
                ImageUrl = album.Images?.FirstOrDefault()?.Url,
                ReleaseDate = album.ReleaseDate ?? string.Empty,
                AlbumType = album.AlbumType ?? string.Empty,
                Uri = album.Uri ?? string.Empty
            };

            _logger.LogInformation("Retrieved album: {AlbumName}", albumInfo.Name);
            return albumInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting album {AlbumId}", albumId);
            return null;
        }
    }
}
