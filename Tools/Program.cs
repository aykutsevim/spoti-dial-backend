using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyAuthHelper;

class Program
{
    private const int CallbackPort = 5000;
    private static readonly string RedirectUri = $"http://localhost:{CallbackPort}/callback";
    private static readonly EmbedIOAuthServer _server = new(new Uri(RedirectUri), CallbackPort);

    static async Task Main(string[] args)
    {
        Console.WriteLine("=========================================");
        Console.WriteLine("  Spotify Refresh Token Generator");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        // Get Client ID and Secret from user
        Console.Write("Enter your Spotify Client ID: ");
        var clientId = Console.ReadLine()?.Trim();

        Console.Write("Enter your Spotify Client Secret: ");
        var clientSecret = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Console.WriteLine("Error: Client ID and Secret are required!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Using redirect URI: {RedirectUri}");
        Console.WriteLine();
        Console.WriteLine("IMPORTANT: Make sure this redirect URI is added to your Spotify app settings!");
        Console.WriteLine("Go to: https://developer.spotify.com/dashboard");
        Console.WriteLine("Edit your app > Settings > Redirect URIs");
        Console.WriteLine();
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();

        try
        {
            // Start the authentication flow
            await StartAuthentication(clientId, clientSecret);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task StartAuthentication(string clientId, string clientSecret)
    {
        // Set up the callback handler
        _server.AuthorizationCodeReceived += async (sender, response) =>
        {
            await _server.Stop();

            try
            {
                var tokenResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, new Uri(RedirectUri))
                );

                Console.WriteLine();
                Console.WriteLine("=========================================");
                Console.WriteLine("  SUCCESS! Here are your tokens:");
                Console.WriteLine("=========================================");
                Console.WriteLine();
                Console.WriteLine($"Access Token: {tokenResponse.AccessToken}");
                Console.WriteLine($"Token Type: {tokenResponse.TokenType}");
                Console.WriteLine($"Expires In: {tokenResponse.ExpiresIn} seconds");
                Console.WriteLine();
                Console.WriteLine("REFRESH TOKEN (copy this to your .env file):");
                Console.WriteLine("=========================================");
                Console.WriteLine(tokenResponse.RefreshToken);
                Console.WriteLine("=========================================");
                Console.WriteLine();
                Console.WriteLine("Add this to your .env file:");
                Console.WriteLine($"SPOTIFY_CLIENT_ID={clientId}");
                Console.WriteLine($"SPOTIFY_CLIENT_SECRET={clientSecret}");
                Console.WriteLine($"SPOTIFY_REFRESH_TOKEN={tokenResponse.RefreshToken}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exchanging code for token: {ex.Message}");
            }
        };

        _server.ErrorReceived += (sender, error, state) =>
        {
            Console.WriteLine($"Authorization error: {error}");
            return Task.CompletedTask;
        };

        await _server.Start();

        // Request the necessary scopes
        var loginRequest = new LoginRequest(
            new Uri(RedirectUri),
            clientId,
            LoginRequest.ResponseType.Code
        )
        {
            Scope = new[]
            {
                Scopes.UserReadPlaybackState,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadCurrentlyPlaying
            }
        };

        var uri = loginRequest.ToUri();

        Console.WriteLine("Opening browser for authorization...");
        Console.WriteLine($"If the browser doesn't open, navigate to: {uri}");
        Console.WriteLine();

        // Try to open the browser
        try
        {
            BrowserUtil.Open(uri);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open browser automatically: {ex.Message}");
            Console.WriteLine($"Please open this URL manually: {uri}");
        }

        Console.WriteLine("Waiting for authorization...");
        Console.WriteLine("(Press Ctrl+C to cancel)");

        // Keep the application running
        await Task.Delay(-1);
    }
}
