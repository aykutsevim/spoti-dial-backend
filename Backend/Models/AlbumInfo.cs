namespace SpotiDialBackend.Models;

public class AlbumInfo
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
