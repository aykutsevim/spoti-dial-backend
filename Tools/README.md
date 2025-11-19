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
   - **Redirect URI**: `http://localhost:5000/callback`
   - **Which API/SDKs are you planning to use?**: Check **Web API**
5. Click **"Save"**
6. In your app settings, note your:
   - **Client ID**
   - **Client Secret** (click "View client secret")

## Step 2: Run the Authentication Helper

From the `Tools` directory, run:

```bash
cd Tools
dotnet run
```

Or from the root directory:

```bash
cd Tools && dotnet run
```

## Step 3: Follow the Prompts

1. The tool will ask for your **Client ID** and **Client Secret**
2. It will open your browser for Spotify authorization
3. Log in to Spotify and authorize the app
4. The tool will display your **Refresh Token**

## Step 4: Update Your .env File

Copy the displayed credentials to your `.env` file in the root directory:

```env
SPOTIFY_CLIENT_ID=your_client_id_here
SPOTIFY_CLIENT_SECRET=your_client_secret_here
SPOTIFY_REFRESH_TOKEN=your_refresh_token_here
```

## Troubleshooting

### "Redirect URI mismatch" error

Make sure you added `http://localhost:5000/callback` to your app's Redirect URIs in the Spotify Developer Dashboard.

### Browser doesn't open automatically

If the browser doesn't open, copy the URL displayed in the console and paste it into your browser manually.

### Port 5000 already in use

If port 5000 is already in use, you can:
1. Stop the service using port 5000
2. Or modify the `CallbackPort` in `Program.cs` to use a different port (and update the redirect URI in Spotify Dashboard accordingly)

## Required Scopes

The tool requests the following Spotify scopes:
- `user-read-playback-state` - Read your current playback state
- `user-modify-playback-state` - Control playback (play, pause, skip, volume)
- `user-read-currently-playing` - Read currently playing track

These scopes are necessary for the SpotiDial Backend to function properly.
