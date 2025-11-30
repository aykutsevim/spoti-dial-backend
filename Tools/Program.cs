using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyAuthHelper;

class Program
{
    private const int CallbackPort = 8888;
    private static readonly string LocalRedirectUri = $"http://127.0.0.1:{CallbackPort}/callback";
    private static EmbedIOAuthServer? _server;

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
        Console.WriteLine("Choose authentication mode:");
        Console.WriteLine("  1. Automatic (opens browser - requires GUI environment)");
        Console.WriteLine("  2. Manual (headless server - you paste the code)");
        Console.Write("Enter choice (1 or 2): ");
        var choice = Console.ReadLine()?.Trim();

        Console.WriteLine();

        try
        {
            if (choice == "2")
            {
                await StartManualAuthentication(clientId, clientSecret);
            }
            else
            {
                Console.WriteLine($"Using redirect URI: {LocalRedirectUri}");
                Console.WriteLine();
                Console.WriteLine("IMPORTANT: Make sure this redirect URI is added to your Spotify app settings!");
                Console.WriteLine("Go to: https://developer.spotify.com/dashboard");
                Console.WriteLine("Edit your app > Settings > Redirect URIs");
                Console.WriteLine($"Add: {LocalRedirectUri}");
                Console.WriteLine();
                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();

                await StartAutomaticAuthentication(clientId, clientSecret);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task StartManualAuthentication(string clientId, string clientSecret)
    {
        const string ManualRedirectUri = "http://localhost:8888/callback";

        Console.WriteLine("HEADLESS SERVER MODE - Manual Authorization");
        Console.WriteLine("=========================================");
        Console.WriteLine();
        Console.WriteLine("IMPORTANT: Make sure this redirect URI is added to your Spotify app settings!");
        Console.WriteLine("Go to: https://developer.spotify.com/dashboard");
        Console.WriteLine("Edit your app > Settings > Redirect URIs");
        Console.WriteLine($"Add: {ManualRedirectUri}");
        Console.WriteLine();
        Console.WriteLine("Press ENTER when you've added the redirect URI...");
        Console.ReadLine();
        Console.WriteLine();

        // Generate the authorization URL
        var loginRequest = new LoginRequest(
            new Uri(ManualRedirectUri),
            clientId,
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

        var authUrl = loginRequest.ToUri();

        Console.WriteLine("STEP 1: Open this URL in your browser:");
        Console.WriteLine("=========================================");
        Console.WriteLine(authUrl);
        Console.WriteLine("=========================================");
        Console.WriteLine();
        Console.WriteLine("STEP 2: After authorizing, you'll be redirected to a URL that starts with:");
        Console.WriteLine($"        {ManualRedirectUri}?code=...");
        Console.WriteLine();
        Console.WriteLine("STEP 3: Copy the ENTIRE URL from your browser's address bar");
        Console.WriteLine("        (or just copy the 'code' parameter value)");
        Console.WriteLine();
        Console.Write("Paste the full callback URL or authorization code here: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Error: No input provided!");
            return;
        }

        // Extract the code from the input
        string? code = null;

        if (input.StartsWith("http"))
        {
            // User pasted the full URL
            var uri = new Uri(input);
            var queryParams = uri.Query.TrimStart('?').Split('&');
            foreach (var param in queryParams)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == "code")
                {
                    code = Uri.UnescapeDataString(keyValue[1]);
                    break;
                }
            }
        }
        else
        {
            // User pasted just the code
            code = input;
        }

        if (string.IsNullOrEmpty(code))
        {
            Console.WriteLine("Error: Could not extract authorization code from input!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Exchanging authorization code for tokens...");

        try
        {
            var tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(clientId, clientSecret, code, new Uri(ManualRedirectUri))
            );

            PrintTokens(clientId, clientSecret, tokenResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exchanging code for token: {ex.Message}");
            Console.WriteLine("Make sure:");
            Console.WriteLine("  1. The redirect URI is correctly set in your Spotify app settings");
            Console.WriteLine("  2. You pasted the complete authorization code");
            Console.WriteLine("  3. You didn't wait too long (codes expire quickly)");
        }
    }

    private static async Task StartAutomaticAuthentication(string clientId, string clientSecret)
    {
        _server = new EmbedIOAuthServer(new Uri(LocalRedirectUri), CallbackPort);

        // Set up the callback handler
        _server.AuthorizationCodeReceived += async (sender, response) =>
        {
            await _server.Stop();

            try
            {
                var tokenResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, new Uri(LocalRedirectUri))
                );

                PrintTokens(clientId, clientSecret, tokenResponse);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exchanging code for token: {ex.Message}");
                Environment.Exit(1);
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
            new Uri(LocalRedirectUri),
            clientId,
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

    private static void PrintTokens(string clientId, string clientSecret, AuthorizationCodeTokenResponse tokenResponse)
    {
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
        Console.WriteLine("After adding these to your .env file, restart the backend service.");
    }
}
