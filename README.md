# Spoti-Dial Backend

A C# backend application for controlling Spotify using an ESP32 M5Dial device.

## Overview

This application creates a bridge between an ESP32 M5Dial device and the Spotify API, allowing physical control of your Spotify playback through the M5Dial interface.

## Features

### MQTT Integration
- Maintains persistent MQTT connection with ESP32 M5Dial device
- Receives real-time commands from the physical device

### Spotify Control
The application supports the following Spotify commands:
- **Playback Control**: Play and pause
- **Track Navigation**: Change songs
- **Collection Navigation**: Switch between playlists and albums
- **Volume Control**: Adjust playback volume

### Device Feedback
- **Song Change Detection**: Monitors Spotify playback and detects song changes
- **Metadata Sync**: Sends current song information to the M5Dial display
- **Album Artwork**: Transmits playlist, album, and song pictures to the device

### Configuration
- Environment-based configuration for easy deployment
- Supports multiple environments through `.env` files

### Deployment
- **Docker Ready**: Fully containerized application
- **Docker Compose**: Orchestration configuration included
- **Example Environment**: Template `.env` file provided for quick setup

## Technology Stack

- **Backend**: C# / .NET
- **Protocol**: MQTT for device communication
- **API**: Spotify Web API
- **Containerization**: Docker & Docker Compose

## Project Structure

```
spoti-dial-backend/
├── Backend/              # Main backend application
│   ├── Services/         # Service implementations (MQTT, Spotify, etc.)
│   ├── Models/           # Data models
│   ├── Program.cs        # Application entry point
│   ├── appsettings.json  # Application configuration
│   └── SpotiDialBackend.csproj
├── CLIClient/            # MQTT testing tool
│   └── CLIClient         # Command-line client for testing backend
├── Tools/                # Helper utilities
│   └── SpotifyAuthHelper # OAuth token generation tool
├── .env                  # Environment configuration (not in git)
├── .env.example          # Environment template
├── docker-compose.yml    # Docker orchestration
└── README.md             # This file
```

## Getting Started

### Prerequisites

- .NET 10 SDK or Docker and Docker Compose installed
- An MQTT broker (e.g., Mosquitto) running and accessible
- A Spotify Premium account
- Spotify Developer application credentials

### Setup Instructions

#### 1. Clone and Configure

```bash
# Copy the example environment file
cp .env.example .env

# Edit .env with your configuration
nano .env
```

#### 2. Set Up Spotify API Credentials

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Click "Create an App"
3. Fill in the app name and description
4. Copy the **Client ID** and **Client Secret** to your `.env` file
5. Add `http://localhost:5000/callback` to Redirect URIs in app settings

#### 3. Obtain Spotify Refresh Token

**Easy Method - Use the Auth Helper Tool:**

We provide a helper tool to easily obtain your Spotify refresh token:

```bash
cd Tools
dotnet run
```

The tool will:
1. Prompt you for your Client ID and Client Secret
2. Open your browser for Spotify authorization
3. Display your refresh token

Copy the refresh token to your `.env` file.

**Manual Method:**

You can also obtain a refresh token manually using the OAuth2 authorization flow. See the [Spotify API documentation](https://developer.spotify.com/documentation/web-api/tutorials/code-flow) or use tools like the [Spotify Web API Auth Examples](https://github.com/spotify/web-api-auth-examples).

Required Spotify scopes:
- `user-read-playback-state`
- `user-modify-playback-state`
- `user-read-currently-playing`

#### 4. Configure MQTT Broker

Update the MQTT settings in `.env`:
```env
MQTT_BROKER_HOST=your-mqtt-broker-host
MQTT_BROKER_PORT=1883
MQTT_USERNAME=your-username  # Optional
MQTT_PASSWORD=your-password  # Optional
```

#### 5. Run the Application

Using Docker Compose:
```bash
docker-compose up -d
```

View logs:
```bash
docker-compose logs -f spotidial-backend
```

Stop the application:
```bash
docker-compose down
```

### Running Without Docker

If you prefer to run without Docker:

```bash
# From the Backend directory
cd Backend

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

Or from the root directory:

```bash
# Build and run using the solution file
dotnet build spoti-dial-backend.sln
dotnet run --project Backend/SpotiDialBackend.csproj
```

## Testing the Backend

### Using the CLI Client

The CLIClient is a command-line tool for testing the backend without needing the physical M5Dial device.

**Quick Start:**

```bash
cd CLIClient

# See all available commands
dotnet run -- --help

# Test playback control
dotnet run -- play
dotnet run -- pause
dotnet run -- next

# Test volume control
dotnet run -- set-volume 75
dotnet run -- volume-up

# Monitor status updates
dotnet run -- monitor
```

**With custom MQTT broker:**

```bash
dotnet run -- play --host mqtt.example.com --port 1883 -u username -pw password
```

For detailed usage and all available commands, see [CLIClient/README.md](CLIClient/README.md).

## MQTT Command Format

Send commands to the ESP32 by publishing to the command topic (`spotidial/commands`):

```json
{
  "command": "play"
}
```

Available commands:
- `play` - Resume playback
- `pause` - Pause playback
- `next` - Next track
- `previous` - Previous track
- `volume_up` - Increase volume by 5%
- `volume_down` - Decrease volume by 5%
- `set_volume` - Set specific volume (requires `parameter` field)
- `change_playlist` - Change to playlist (requires `parameter` with playlist ID)
- `change_album` - Change to album (requires `parameter` with album ID)

Example with parameter:
```json
{
  "command": "set_volume",
  "parameter": "75"
}
```

## Status Updates

The backend publishes song information to `spotidial/status`:

```json
{
  "trackName": "Song Name",
  "artistName": "Artist Name",
  "albumName": "Album Name",
  "durationMs": 240000,
  "progressMs": 60000,
  "isPlaying": true,
  "volumePercent": 80,
  "albumImageUrl": "https://..."
}
```

Album artwork is published as JPEG binary data to `spotidial/image`.

## Architecture

```
ESP32 M5Dial ←→ MQTT Broker ←→ Spoti-Dial Backend ←→ Spotify API
```

The backend acts as a mediator, translating physical dial interactions into Spotify API calls and pushing playback information back to the device display.
