#ifndef MQTT_CLIENT_H
#define MQTT_CLIENT_H

#include <PubSubClient.h>
#include <WiFi.h>
#include <ArduinoJson.h>
#include "config.h"

// Callback types
typedef void (*StatusCallback)(const char* trackName, const char* artistName,
                               const char* albumName, int progressMs, int durationMs,
                               int volumePercent, bool isPlaying);
typedef void (*ImageCallback)(uint8_t* imageData, size_t length);
typedef void (*PlaylistsCallback)(JsonArray playlists);
typedef void (*AlbumsCallback)(JsonArray albums);

class MQTTClient {
public:
    MQTTClient();

    // Initialization and connection
    bool begin();
    void loop();
    bool isConnected();

    // Command publishing
    void sendCommand(const char* command, const char* parameter = nullptr);
    void play();
    void pause();
    void nextTrack();
    void previousTrack();
    void volumeUp();
    void volumeDown();
    void setVolume(int volume);
    void changePlaylist(const char* playlistId);
    void changeAlbum(const char* albumId);
    void getPlaylists();
    void getAlbums();

    // Callbacks
    void onStatus(StatusCallback callback) { _statusCallback = callback; }
    void onImage(ImageCallback callback) { _imageCallback = callback; }
    void onPlaylists(PlaylistsCallback callback) { _playlistsCallback = callback; }
    void onAlbums(AlbumsCallback callback) { _albumsCallback = callback; }

private:
    WiFiClient _wifiClient;
    PubSubClient _mqttClient;

    StatusCallback _statusCallback;
    ImageCallback _imageCallback;
    PlaylistsCallback _playlistsCallback;
    AlbumsCallback _albumsCallback;

    unsigned long _lastReconnectAttempt;

    // Connection helpers
    bool reconnect();
    void subscribe();

    // Message handling
    static void messageCallback(char* topic, uint8_t* payload, unsigned int length);
    void handleStatusMessage(uint8_t* payload, unsigned int length);
    void handleImageMessage(uint8_t* payload, unsigned int length);
    void handlePlaylistsMessage(uint8_t* payload, unsigned int length);
    void handleAlbumsMessage(uint8_t* payload, unsigned int length);

    // Static instance for callback
    static MQTTClient* _instance;
};

#endif // MQTT_CLIENT_H
