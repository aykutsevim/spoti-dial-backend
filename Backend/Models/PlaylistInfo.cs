namespace SpotiDialBackend.Models;

public class PlaylistInfo
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
