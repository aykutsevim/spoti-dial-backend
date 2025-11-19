#ifndef CONFIG_H
#define CONFIG_H

// ============================================
// WiFi Configuration
// ============================================
// These will be set via WiFiManager on first boot
// Default fallback values
#define WIFI_SSID ""
#define WIFI_PASSWORD ""
#define WIFI_TIMEOUT_MS 20000

// ============================================
// MQTT Configuration
// ============================================
#define MQTT_BROKER "192.168.1.100"  // Change to your MQTT broker IP
#define MQTT_PORT 1883
#define MQTT_CLIENT_ID "M5Dial-SpotiDial"
#define MQTT_USERNAME ""
#define MQTT_PASSWORD ""

// MQTT Topics (must match backend configuration)
#define MQTT_TOPIC_COMMAND "spotidial/commands"
#define MQTT_TOPIC_STATUS "spotidial/status"
#define MQTT_TOPIC_IMAGE "spotidial/image"
#define MQTT_TOPIC_PLAYLISTS "spotidial/playlists"
#define MQTT_TOPIC_ALBUMS "spotidial/albums"

// MQTT Settings
#define MQTT_RECONNECT_DELAY 5000
#define MQTT_KEEPALIVE 60

// ============================================
// Display Configuration
// ============================================
#define DISPLAY_WIDTH 240
#define DISPLAY_HEIGHT 240
#define DISPLAY_ROTATION 0

// UI Update intervals (milliseconds)
#define UI_UPDATE_INTERVAL 100
#define STATUS_UPDATE_INTERVAL 1000

// ============================================
// Encoder Configuration
// ============================================
#define ENCODER_STEPS_PER_DETENT 4
#define ENCODER_VOLUME_STEP 5  // Volume change per encoder step
#define ENCODER_DEBOUNCE_MS 50

// ============================================
// Application Settings
// ============================================
#define APP_NAME "SpotiDial"
#define APP_VERSION "1.0.0"

// Debug settings
#define DEBUG_SERIAL_ENABLE true
#define DEBUG_MQTT_MESSAGES true

// ============================================
// LVGL Configuration
// ============================================
#define LVGL_TICK_PERIOD 5
#define LVGL_BUFFER_SIZE (DISPLAY_WIDTH * DISPLAY_HEIGHT / 10)

#endif // CONFIG_H
