using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SpotiDialBackend.Services;

public class TokenStorageService
{
    private readonly ILogger<TokenStorageService> _logger;
    private readonly string _tokenFilePath;

    public TokenStorageService(ILogger<TokenStorageService> logger)
    {
        _logger = logger;
        _tokenFilePath = Path.Combine(AppContext.BaseDirectory, "spotify_token.json");
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                _logger.LogInformation("No stored refresh token found");
                return null;
            }

            var json = await File.ReadAllTextAsync(_tokenFilePath);
            var tokenData = JsonSerializer.Deserialize<TokenData>(json);

            if (tokenData?.RefreshToken != null)
            {
                _logger.LogInformation("Loaded refresh token from storage");
                return tokenData.RefreshToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading refresh token from storage");
            return null;
        }
    }

    public async Task SaveRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenData = new TokenData
            {
                RefreshToken = refreshToken,
                SavedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_tokenFilePath, json);
            _logger.LogInformation("Refresh token saved to storage at {Path}", _tokenFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving refresh token to storage");
        }
    }

    private class TokenData
    {
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}
