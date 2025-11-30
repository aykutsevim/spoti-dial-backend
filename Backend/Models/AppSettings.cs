namespace SpotiDialBackend.Models;

public class AppSettings
{
    public MqttSettings Mqtt { get; set; } = new();
    public SpotifySettings Spotify { get; set; } = new();
}

public class MqttSettings
{
    public string BrokerHost { get; set; } = string.Empty;
    public int BrokerPort { get; set; } = 1883;
    public string ClientId { get; set; } = "SpotiDialBackend";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CommandTopic { get; set; } = "spotidial/commands";
    public string StatusTopic { get; set; } = "spotidial/status";
    public string ImageTopic { get; set; } = "spotidial/image";
    public string PlaylistTopic { get; set; } = "spotidial/playlists";
    public string AlbumTopic { get; set; } = "spotidial/albums";
}

public class SpotifySettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int PollingIntervalMs { get; set; } = 1000;
    public int OAuthCallbackPort { get; set; } = 8888;

    private string? _oauthRedirectUri;
    public string OAuthRedirectUri
    {
        get => _oauthRedirectUri ?? $"http://127.0.0.1:{OAuthCallbackPort}/callback";
        set => _oauthRedirectUri = value;
    }
}
