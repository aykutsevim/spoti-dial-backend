namespace SpotiDialBackend.Models;

public class SongInfo
{
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public int ProgressMs { get; set; }
    public bool IsPlaying { get; set; }
    public int VolumePercent { get; set; }
    public string? AlbumImageUrl { get; set; }
}
