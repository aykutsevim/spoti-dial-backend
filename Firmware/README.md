# M5Dial SpotiDial Firmware

ESP32-S3 based firmware for the M5Dial device to control Spotify playback.

## Features

- 🎵 Real-time now playing display
- 🎨 Album artwork display
- 🔊 Volume control with rotary encoder
- 📋 Browse playlists and albums
- ⏯️ Playback control (play, pause, next, previous)
- 🔄 Automatic reconnection to WiFi and MQTT
- 📱 WiFi configuration via captive portal

## Hardware Requirements

- **M5Dial** (ESP32-S3 based device)
  - 1.28" round touch display (240x240)
  - Rotary encoder with button
  - WiFi connectivity

## Software Requirements

- [PlatformIO](https://platformio.org/) or [PlatformIO IDE for VSCode](https://platformio.org/install/ide?install=vscode)
- USB-C cable for programming

## Project Structure

```
Firmware/
├── platformio.ini          # PlatformIO configuration
├── src/
│   ├── main.cpp           # Main application entry point
│   ├── config/
│   │   └── config.h       # Configuration constants
│   ├── mqtt/
│   │   ├── mqtt_client.h  # MQTT client header
│   │   └── mqtt_client.cpp # MQTT client implementation
│   └── ui/
│       ├── ui_manager.h   # UI manager header
│       └── ui_manager.cpp # UI manager implementation
├── include/
│   ├── config.h           # Global configuration
│   └── lv_conf.h          # LVGL configuration
├── lib/                   # Custom libraries (if any)
└── data/                  # Data files (images, fonts, etc.)
```

## Configuration

### 1. MQTT Broker Settings

Edit `include/config.h` and update:

```cpp
#define MQTT_BROKER "192.168.1.100"  // Your MQTT broker IP
#define MQTT_PORT 1883
#define MQTT_USERNAME ""              // If authentication required
#define MQTT_PASSWORD ""              // If authentication required
```

### 2. WiFi Configuration

On first boot, the device will create a WiFi access point:
- SSID: `M5Dial-SpotiDial`
- Connect and configure your WiFi credentials via the captive portal
- Optionally set MQTT broker IP

## Building and Uploading

### Using PlatformIO CLI:

```bash
# Navigate to Firmware directory
cd Firmware

# Build the project
pio run

# Upload to device
pio run --target upload

# Monitor serial output
pio device monitor
```

### Using VSCode:

1. Open the `Firmware` folder in VSCode
2. PlatformIO should auto-detect the project
3. Click the PlatformIO icon in the sidebar
4. Click "Build" to compile
5. Click "Upload" to flash the device
6. Click "Monitor" to view serial output

## Usage

### Controls

**Rotary Encoder:**
- **Rotate** - Adjust volume (on Now Playing screen)
- **Rotate** - Scroll through lists (on Playlists/Albums screens)
- **Click** - Toggle play/pause (on Now Playing screen)
- **Click** - Select item (on Playlists/Albums screens)
- **Long Press (1s)** - Cycle through screens

### Screens

1. **Now Playing** - Shows current track, artist, album art, and playback controls
2. **Playlists** - Browse and select playlists
3. **Albums** - Browse and select saved albums
4. **Settings** - Configure device settings

## MQTT Topics

The firmware communicates with the backend using these topics:

**Subscribe (Receive):**
- `spotidial/status` - Current playback status
- `spotidial/image` - Album artwork
- `spotidial/playlists` - Playlist list
- `spotidial/albums` - Album list

**Publish (Send):**
- `spotidial/commands` - Control commands (play, pause, next, etc.)

## Commands

The firmware can send these commands:

```json
{"command": "play"}
{"command": "pause"}
{"command": "next"}
{"command": "previous"}
{"command": "volume_up"}
{"command": "volume_down"}
{"command": "set_volume", "parameter": "75"}
{"command": "change_playlist", "parameter": "playlist_id"}
{"command": "change_album", "parameter": "album_id"}
{"command": "get_playlists"}
{"command": "get_albums"}
```

## Debugging

### Serial Monitor

Connect to the device at 115200 baud to see debug output:

```bash
pio device monitor -b 115200
```

### Debug Flags

Edit `include/config.h`:

```cpp
#define DEBUG_SERIAL_ENABLE true
#define DEBUG_MQTT_MESSAGES true
```

## Troubleshooting

### WiFi Issues

1. **Can't connect to WiFi**
   - Hold the button for 10 seconds to reset WiFi settings
   - Device will create AP mode again
   - Reconfigure WiFi credentials

2. **WiFi keeps disconnecting**
   - Check signal strength
   - Ensure 2.4GHz WiFi is being used (ESP32 doesn't support 5GHz)

### MQTT Issues

1. **Can't connect to MQTT broker**
   - Verify MQTT broker IP in `config.h`
   - Check that mosquitto is running: `docker-compose ps`
   - Verify firewall settings

2. **Not receiving updates**
   - Check MQTT topics match backend configuration
   - Monitor MQTT messages: `mosquitto_sub -h localhost -t "spotidial/#" -v`

### Display Issues

1. **Display not working**
   - Check M5Dial library version
   - Verify LVGL configuration in `lv_conf.h`

2. **UI is slow**
   - Reduce `LV_MEM_SIZE` if memory is low
   - Disable debug logging for production

## Development

### Adding New Features

1. **New UI Screen:**
   - Add to `UIScreen` enum in `ui_manager.h`
   - Create screen in `ui_manager.cpp`
   - Handle encoder input for the screen

2. **New MQTT Command:**
   - Add method to `MQTTClient` class
   - Call from appropriate UI handler

3. **Custom Widgets:**
   - Create in `src/ui/widgets/`
   - Include LVGL widget definitions

## Performance Tips

- Keep LVGL buffer size reasonable (currently 1/10 of display)
- Use `LV_COLOR_DEPTH 16` for better performance
- Minimize frequent screen updates
- Use LVGL animations sparingly

## Dependencies

All dependencies are managed by PlatformIO:

- **M5Dial** - M5Stack M5Dial library
- **LVGL** - Light and Versatile Graphics Library
- **PubSubClient** - MQTT client
- **ArduinoJson** - JSON parsing
- **WiFiManager** - WiFi configuration

## License

Part of the SpotiDial project. See main repository for license information.

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing style
- Test on actual hardware
- Update this README if adding features
