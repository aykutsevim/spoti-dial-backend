# Spotify Authentication Helper

This tool helps you obtain a Spotify refresh token for the SpotiDial Backend application.

## Prerequisites

1. A Spotify account (Free or Premium)
2. A Spotify Developer App

## Step 1: Create a Spotify App

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Log in with your Spotify account
3. Click **"Create app"**
4. Fill in the details:
   - **App name**: `SpotiDial Backend` (or any name you prefer)
   - **App description**: `MQTT bridge for Spotify control`
   - **Redirect URI**: See Step 2 below (depends on which mode you'll use)
   - **Which API/SDKs are you planning to use?**: Check **Web API**
5. Click **"Save"**
6. In your app settings, note your:
   - **Client ID**
   - **Client Secret** (click "View client secret")

## Step 2: Run the Authentication Helper

The tool supports two modes:

### Option 1: Automatic Mode (Local Machine with GUI)
Best for running on your local computer with a browser available.
- **Redirect URI to add**: `http://127.0.0.1:8888/callback`

### Option 2: Manual Mode (Headless Server)
Best for remote servers without GUI or browser access.
- **Redirect URI to add**: `http://localhost:8888/callback`

From the `Tools` directory, run:

```bash
cd Tools
dotnet run
```

Or from the root directory:

```bash
cd Tools && dotnet run
```

## Step 3: Choose Your Mode

The tool will prompt you to choose:
- **Option 1 - Automatic**: Opens browser automatically (requires GUI environment)
- **Option 2 - Manual**: Displays URL for you to copy and paste (for headless servers)

### Automatic Mode (Option 1)

1. Enter your **Client ID** and **Client Secret**
2. The tool will open your browser automatically
3. Log in to Spotify and authorize the app
4. The tool will automatically capture the authorization and display your **Refresh Token**

### Manual Mode (Option 2) - For Headless Servers

1. Enter your **Client ID** and **Client Secret**
2. The tool will display an authorization URL
3. **Copy the URL** and open it on ANY device (phone, laptop, etc.)
4. Log in to Spotify and authorize the app
5. After authorization, you'll be redirected to a page that won't load (that's expected!)
6. **Copy the ENTIRE URL** from your browser's address bar (it starts with `http://localhost:8888/callback?code=...`)
7. **Paste the URL** back into the tool
8. The tool will extract the authorization code and display your **Refresh Token**

## Step 4: Update Your .env File

Copy the displayed credentials to your `.env` file in the root directory:

```env
SPOTIFY_CLIENT_ID=your_client_id_here
SPOTIFY_CLIENT_SECRET=your_client_secret_here
SPOTIFY_REFRESH_TOKEN=your_refresh_token_here
```

## Troubleshooting

### "Redirect URI mismatch" error

Make sure you added the correct redirect URI to your app's Redirect URIs in the Spotify Developer Dashboard:
- For **Automatic Mode**: `http://127.0.0.1:8888/callback`
- For **Manual Mode**: `http://localhost:8888/callback`

You can add both if you plan to use the tool in different environments.

### Browser doesn't open automatically (Automatic Mode)

If the browser doesn't open, copy the URL displayed in the console and paste it into your browser manually.

### Port 8888 already in use

If port 8888 is already in use, you can:
1. Stop the service using port 8888
2. Or modify the `CallbackPort` in `Program.cs` to use a different port (and update the redirect URI in Spotify Dashboard accordingly)

### Authorization code expired (Manual Mode)

If you get an error about the authorization code being invalid:
1. Make sure you copy the ENTIRE callback URL including `?code=...`
2. Try again quickly - authorization codes expire after a few minutes
3. Don't refresh the redirect page before copying the URL

### "Error exchanging code for token" (Manual Mode)

Make sure:
1. The redirect URI (`http://localhost:8888/callback`) is added to your Spotify app settings
2. You copied the complete authorization code from the URL
3. You didn't wait too long (codes expire quickly)

## Required Scopes

The tool requests the following Spotify scopes:
- `user-read-playback-state` - Read your current playback state
- `user-modify-playback-state` - Control playback (play, pause, skip, volume)
- `user-read-currently-playing` - Read currently playing track
- `playlist-read-private` - Access your private playlists
- `playlist-read-collaborative` - Access collaborative playlists
- `user-library-read` - Access your saved albums

These scopes are necessary for the SpotiDial Backend to function properly.

## Using with Headless Server

Once you have your refresh token:

1. Add it to your `.env` file on the headless server:
   ```env
   SPOTIFY_CLIENT_ID=your_client_id_here
   SPOTIFY_CLIENT_SECRET=your_client_secret_here
   SPOTIFY_REFRESH_TOKEN=your_refresh_token_here
   ```

2. Start the backend service:
   ```bash
   docker-compose up -d
   ```

The backend will use the refresh token to automatically obtain and refresh access tokens as needed. You won't need to authorize again unless you revoke the token.
