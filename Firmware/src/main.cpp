#include <M5Dial.h>
#include <WiFi.h>
#include <WiFiManager.h>
#include "config.h"
#include "mqtt/mqtt_client.h"
#include "ui/ui_manager.h"
#include "lv_display.h"

// Global objects
MQTTClient mqttClient;
UIManager uiManager;
WiFiManager wifiManager;

// State variables
int currentVolume = 50;
long oldEncoderPosition = 0;
unsigned long lastEncoderTime = 0;
bool encoderPressed = false;

// Function declarations
void setupWiFi();
void handleEncoder();
void onStatusUpdate(const char* trackName, const char* artistName,
                   const char* albumName, int progressMs, int durationMs,
                   int volumePercent, bool isPlaying);
void onImageUpdate(uint8_t* imageData, size_t length);
void onPlaylistsUpdate(JsonArray playlists);
void onAlbumsUpdate(JsonArray albums);

void setup() {
    // Initialize Serial for debugging (FIRST!)
    Serial.begin(115200);
    delay(500);  // Give serial time to initialize
    Serial.println("\n\n" APP_NAME " v" APP_VERSION);
    Serial.println("================================");

    // Initialize M5Dial
    Serial.println("Initializing M5Dial...");
    auto cfg = M5.config();
    M5Dial.begin(cfg, true, false);
    Serial.println("M5Dial initialized");

    // Initialize LVGL with M5Dial display
    Serial.println("Initializing LVGL...");
    if (!lv_display_init()) {
        Serial.println("Failed to initialize LVGL!");
        while (1) delay(100);
    }
    Serial.println("LVGL initialized");

    // Initialize UI
    Serial.println("Initializing UI...");
    if (!uiManager.begin()) {
        Serial.println("Failed to initialize UI!");
        while (1) delay(100);
    }
    Serial.println("UI initialized");

    // Show splash screen
    uiManager.showScreen(SCREEN_SPLASH);
    Serial.println("Splash screen shown");

    // Setup WiFi (delayed after UI is ready)
    Serial.println("Setting up WiFi...");
    setupWiFi();
    Serial.println("WiFi setup complete");

    // Initialize MQTT
    Serial.println("Connecting to MQTT broker...");
    mqttClient.begin();

    // Register MQTT callbacks
    mqttClient.onStatus(onStatusUpdate);
    mqttClient.onImage(onImageUpdate);
    mqttClient.onPlaylists(onPlaylistsUpdate);
    mqttClient.onAlbums(onAlbumsUpdate);

    // Show now playing screen
    uiManager.showScreen(SCREEN_NOW_PLAYING);

    Serial.println("Setup complete!");
    Serial.println("================================\n");
}

void loop() {
    M5Dial.update();

    // Handle MQTT
    mqttClient.loop();

    // Handle encoder
    handleEncoder();

    // Update UI
    uiManager.update();

    // Update LVGL tick timer (for animations and timers)
    lv_tick_inc(5);

    // Small delay to prevent overwhelming the system
    delay(5);
}

void setupWiFi() {
    // Set WiFiManager timeout to 30 seconds (shorter than 180)
    wifiManager.setConfigPortalTimeout(30);

    // Reduce time for WiFi connection attempts
    wifiManager.setConnectTimeout(20);

    // Custom parameters for MQTT broker (optional)
    WiFiManagerParameter custom_mqtt_broker("broker", "MQTT Broker IP", MQTT_BROKER, 40);
    wifiManager.addParameter(&custom_mqtt_broker);

    // Try to connect with saved credentials (don't restart on failure)
    if (wifiManager.autoConnect("M5Dial-SpotiDial")) {
        Serial.println("WiFi connected!");
        Serial.print("IP address: ");
        Serial.println(WiFi.localIP());

        // Save MQTT broker if changed
        String mqttBroker = custom_mqtt_broker.getValue();
        if (mqttBroker.length() > 0) {
            Serial.print("MQTT Broker: ");
            Serial.println(mqttBroker);
            // TODO: Save to preferences/EEPROM
        }
    } else {
        Serial.println("WiFi connection failed - will try again in background");
    }
}

void handleEncoder() {
    long newPosition = M5Dial.Encoder.read();

    // Handle rotation
    if (newPosition != oldEncoderPosition) {
        int delta = newPosition - oldEncoderPosition;
        oldEncoderPosition = newPosition;

        // Debounce encoder
        if (millis() - lastEncoderTime < ENCODER_DEBOUNCE_MS) {
            return;
        }
        lastEncoderTime = millis();

        // Handle based on current screen
        UIScreen currentScreen = uiManager.getCurrentScreen();

        switch (currentScreen) {
            case SCREEN_NOW_PLAYING:
                // Control volume with encoder
                currentVolume += (delta * ENCODER_VOLUME_STEP);
                currentVolume = constrain(currentVolume, 0, 100);
                mqttClient.setVolume(currentVolume);
                uiManager.showVolumeOverlay(currentVolume);
                break;

            case SCREEN_PLAYLISTS:
            case SCREEN_ALBUMS:
                // Scroll through lists
                uiManager.onEncoderChange(delta);
                break;

            default:
                break;
        }
    }

    // Handle button press
    if (M5Dial.BtnA.wasPressed()) {
        Serial.println("Button pressed");

        UIScreen currentScreen = uiManager.getCurrentScreen();

        switch (currentScreen) {
            case SCREEN_NOW_PLAYING:
                // Toggle play/pause
                // TODO: Track playing state
                mqttClient.play(); // or pause()
                break;

            case SCREEN_PLAYLISTS:
            case SCREEN_ALBUMS:
                // Select item
                uiManager.onEncoderClick();
                break;

            default:
                break;
        }
    }

    // Long press to change screens
    if (M5Dial.BtnA.pressedFor(1000) && !encoderPressed) {
        encoderPressed = true;
        Serial.println("Button long pressed - cycling screens");

        UIScreen currentScreen = uiManager.getCurrentScreen();
        UIScreen nextScreen = SCREEN_NOW_PLAYING;

        switch (currentScreen) {
            case SCREEN_NOW_PLAYING:
                nextScreen = SCREEN_PLAYLISTS;
                mqttClient.getPlaylists();
                break;
            case SCREEN_PLAYLISTS:
                nextScreen = SCREEN_ALBUMS;
                mqttClient.getAlbums();
                break;
            case SCREEN_ALBUMS:
                nextScreen = SCREEN_SETTINGS;
                break;
            case SCREEN_SETTINGS:
                nextScreen = SCREEN_NOW_PLAYING;
                break;
            default:
                break;
        }

        uiManager.showScreen(nextScreen);
    }

    if (!M5Dial.BtnA.isPressed()) {
        encoderPressed = false;
    }
}

void onStatusUpdate(const char* trackName, const char* artistName,
                   const char* albumName, int progressMs, int durationMs,
                   int volumePercent, bool isPlaying) {
    Serial.println("Status update received:");
    Serial.printf("  Track: %s\n", trackName);
    Serial.printf("  Artist: %s\n", artistName);
    Serial.printf("  Album: %s\n", albumName);
    Serial.printf("  Progress: %d/%d ms\n", progressMs, durationMs);
    Serial.printf("  Volume: %d%%\n", volumePercent);
    Serial.printf("  Playing: %s\n", isPlaying ? "Yes" : "No");

    // Update current volume
    currentVolume = volumePercent;

    // Update UI
    uiManager.updateNowPlaying(trackName, artistName, albumName,
                               progressMs, durationMs, volumePercent, isPlaying);
}

void onImageUpdate(uint8_t* imageData, size_t length) {
    Serial.printf("Image update received: %d bytes\n", length);
    uiManager.updateAlbumArt(imageData, length);
}

void onPlaylistsUpdate(JsonArray playlists) {
    Serial.printf("Playlists update received: %d playlists\n", playlists.size());

    // Convert to array of strings for UI
    // TODO: Implement proper playlist display
    for (JsonVariant playlist : playlists) {
        const char* name = playlist["name"];
        Serial.printf("  - %s\n", name);
    }
}

void onAlbumsUpdate(JsonArray albums) {
    Serial.printf("Albums update received: %d albums\n", albums.size());

    // Convert to array of strings for UI
    // TODO: Implement proper album display
    for (JsonVariant album : albums) {
        const char* name = album["name"];
        const char* artist = album["artist"];
        Serial.printf("  - %s by %s\n", name, artist);
    }
}
