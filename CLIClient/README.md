# SpotiDial MQTT CLI Client

A command-line tool for testing the SpotiDial backend by simulating MQTT commands from an M5Dial device.

## Features

- Send Spotify control commands via MQTT
- Monitor real-time status updates from the backend
- Test the backend without needing physical M5Dial hardware
- Support for all SpotiDial commands

## Prerequisites

- .NET 10 SDK installed
- An MQTT broker running (e.g., Mosquitto)
- SpotiDial Backend running

## Installation

From the CLIClient directory:

```bash
cd CLIClient
dotnet build
```

## Usage

### Basic Command Format

```bash
dotnet run -- [command] [options]
```

### Global Options

All commands support these options:

- `-h, --host <host>` - MQTT broker host (default: localhost)
- `-p, --port <port>` - MQTT broker port (default: 1883)
- `-u, --username <username>` - MQTT username (optional)
- `-pw, --password <password>` - MQTT password (optional)

### Available Commands

#### Playback Control

**Play**
```bash
dotnet run -- play
```

**Pause**
```bash
dotnet run -- pause
```

**Next Track**
```bash
dotnet run -- next
```

**Previous Track**
```bash
dotnet run -- previous
```

#### Volume Control

**Volume Up** (increase by 5%)
```bash
dotnet run -- volume-up
```

**Volume Down** (decrease by 5%)
```bash
dotnet run -- volume-down
```

**Set Volume** (0-100)
```bash
dotnet run -- set-volume 75
```

#### Playlist/Album Control

**Change Playlist**
```bash
dotnet run -- change-playlist <playlist-id>
```

Example:
```bash
dotnet run -- change-playlist 37i9dQZF1DXcBWIGoYBM5M
```

**Change Album**
```bash
dotnet run -- change-album <album-id>
```

Example:
```bash
dotnet run -- change-album 6QaVfG1pHYl1z15ZxkvVDW
```

#### Monitor Mode

**Monitor Updates**

Subscribe to status and image updates from the backend:

```bash
dotnet run -- monitor
```

This will display:
- Song information updates in real-time
- Album artwork size notifications
- Press Ctrl+C to stop monitoring

### Examples with Custom MQTT Broker

**Connect to a remote MQTT broker:**

```bash
dotnet run -- play --host mqtt.example.com --port 1883
```

**With authentication:**

```bash
dotnet run -- pause --host mqtt.example.com -u myuser -pw mypassword
```

**Monitor with custom broker:**

```bash
dotnet run -- monitor -h 192.168.1.100 -p 1883 -u mqttuser -pw mqttpass
```

## Command Reference

| Command | Description | Parameters |
|---------|-------------|------------|
| `play` | Resume playback | None |
| `pause` | Pause playback | None |
| `next` | Skip to next track | None |
| `previous` | Skip to previous track | None |
| `volume-up` | Increase volume by 5% | None |
| `volume-down` | Decrease volume by 5% | None |
| `set-volume` | Set specific volume | `level` (0-100) |
| `change-playlist` | Change to playlist | `playlist-id` |
| `change-album` | Change to album | `album-id` |
| `monitor` | Monitor updates | None |

## MQTT Message Format

The CLI client sends JSON messages to the `spotidial/commands` topic:

**Simple commands:**
```json
{
  "command": "play",
  "parameter": null
}
```

**Commands with parameters:**
```json
{
  "command": "set_volume",
  "parameter": "75"
}
```

## Monitoring Output

When using `monitor` mode, you'll see output like:

```
[14:32:15] STATUS UPDATE:
{
  "trackName": "Bohemian Rhapsody",
  "artistName": "Queen",
  "albumName": "A Night at the Opera",
  "durationMs": 354000,
  "progressMs": 45000,
  "isPlaying": true,
  "volumePercent": 80,
  "albumImageUrl": "https://..."
}

[14:32:16] IMAGE UPDATE: Received 15234 bytes
```

## Troubleshooting

### Connection Issues

**Error: Connection refused**
- Ensure the MQTT broker is running
- Check the host and port are correct
- Verify firewall settings

**Error: Authentication failed**
- Verify username and password are correct
- Check MQTT broker authentication configuration

### No Response from Backend

- Ensure the SpotiDial backend is running
- Check that the backend is connected to the same MQTT broker
- Verify topic names match (default: `spotidial/commands`, `spotidial/status`)

### Command Not Working

- Use `monitor` mode to see if the backend is responding
- Check backend logs for errors
- Verify your Spotify credentials are configured correctly in the backend

## Testing Workflow

1. **Start your MQTT broker:**
   ```bash
   # Example with Mosquitto
   mosquitto -v
   ```

2. **Start the SpotiDial backend:**
   ```bash
   cd Backend
   dotnet run
   ```

3. **Open a new terminal for monitoring:**
   ```bash
   cd CLIClient
   dotnet run -- monitor
   ```

4. **Open another terminal to send commands:**
   ```bash
   cd CLIClient
   dotnet run -- play
   dotnet run -- set-volume 50
   dotnet run -- next
   ```

5. **Watch the monitor terminal for status updates**

## Integration with CI/CD

You can use this CLI client in automated tests:

```bash
#!/bin/bash
# Test script example

# Start services (docker-compose, etc.)
# ...

# Test basic playback control
dotnet run --project CLIClient -- play --host localhost
sleep 2
dotnet run --project CLIClient -- pause --host localhost

# Test volume control
dotnet run --project CLIClient -- set-volume 50 --host localhost

# Add assertions as needed
```

## Development

To modify or extend the CLI client:

1. Edit `Program.cs` to add new commands
2. Follow the existing pattern for command definitions
3. Build and test:
   ```bash
   dotnet build
   dotnet run -- [your-command]
   ```

## Help

To see all available commands and options:

```bash
dotnet run -- --help
```

To see help for a specific command:

```bash
dotnet run -- [command] --help
```
