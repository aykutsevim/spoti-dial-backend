namespace SpotiDialBackend.Models;

public class DeviceCommand
{
    public string Command { get; set; } = string.Empty;
    public string? Parameter { get; set; }
}

public static class Commands
{
    public const string Play = "play";
    public const string Pause = "pause";
    public const string NextTrack = "next";
    public const string PreviousTrack = "previous";
    public const string VolumeUp = "volume_up";
    public const string VolumeDown = "volume_down";
    public const string SetVolume = "set_volume";
    public const string ChangePlaylist = "change_playlist";
    public const string ChangeAlbum = "change_album";
    public const string GetPlaylists = "get_playlists";
    public const string GetAlbums = "get_albums";
}
