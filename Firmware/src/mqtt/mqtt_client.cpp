#include "mqtt_client.h"

MQTTClient* MQTTClient::_instance = nullptr;

MQTTClient::MQTTClient()
    : _mqttClient(_wifiClient),
      _statusCallback(nullptr),
      _imageCallback(nullptr),
      _playlistsCallback(nullptr),
      _albumsCallback(nullptr),
      _lastReconnectAttempt(0) {
    _instance = this;
}

bool MQTTClient::begin() {
    _mqttClient.setServer(MQTT_BROKER, MQTT_PORT);
    _mqttClient.setCallback(messageCallback);
    _mqttClient.setBufferSize(MQTT_MAX_PACKET_SIZE);
    _mqttClient.setKeepAlive(MQTT_KEEPALIVE);

    return reconnect();
}

void MQTTClient::loop() {
    if (!_mqttClient.connected()) {
        unsigned long now = millis();
        if (now - _lastReconnectAttempt > MQTT_RECONNECT_DELAY) {
            _lastReconnectAttempt = now;
            if (reconnect()) {
                _lastReconnectAttempt = 0;
            }
        }
    } else {
        _mqttClient.loop();
    }
}

bool MQTTClient::isConnected() {
    return _mqttClient.connected();
}

bool MQTTClient::reconnect() {
    Serial.print("Connecting to MQTT broker...");

    bool connected = false;
    if (strlen(MQTT_USERNAME) > 0) {
        connected = _mqttClient.connect(MQTT_CLIENT_ID, MQTT_USERNAME, MQTT_PASSWORD);
    } else {
        connected = _mqttClient.connect(MQTT_CLIENT_ID);
    }

    if (connected) {
        Serial.println(" Connected!");
        subscribe();
        return true;
    } else {
        Serial.print(" Failed, rc=");
        Serial.println(_mqttClient.state());
        return false;
    }
}

void MQTTClient::subscribe() {
    _mqttClient.subscribe(MQTT_TOPIC_STATUS);
    _mqttClient.subscribe(MQTT_TOPIC_IMAGE);
    _mqttClient.subscribe(MQTT_TOPIC_PLAYLISTS);
    _mqttClient.subscribe(MQTT_TOPIC_ALBUMS);

    Serial.println("Subscribed to all topics");
}

void MQTTClient::messageCallback(char* topic, uint8_t* payload, unsigned int length) {
    if (_instance) {
        if (strcmp(topic, MQTT_TOPIC_STATUS) == 0) {
            _instance->handleStatusMessage(payload, length);
        } else if (strcmp(topic, MQTT_TOPIC_IMAGE) == 0) {
            _instance->handleImageMessage(payload, length);
        } else if (strcmp(topic, MQTT_TOPIC_PLAYLISTS) == 0) {
            _instance->handlePlaylistsMessage(payload, length);
        } else if (strcmp(topic, MQTT_TOPIC_ALBUMS) == 0) {
            _instance->handleAlbumsMessage(payload, length);
        }
    }
}

void MQTTClient::handleStatusMessage(uint8_t* payload, unsigned int length) {
    if (!_statusCallback) return;

    StaticJsonDocument<1024> doc;
    DeserializationError error = deserializeJson(doc, payload, length);

    if (error) {
        Serial.print("JSON parse error: ");
        Serial.println(error.c_str());
        return;
    }

    const char* trackName = doc["trackName"] | "Unknown";
    const char* artistName = doc["artistName"] | "Unknown";
    const char* albumName = doc["albumName"] | "Unknown";
    int progressMs = doc["progressMs"] | 0;
    int durationMs = doc["durationMs"] | 0;
    int volumePercent = doc["volumePercent"] | 0;
    bool isPlaying = doc["isPlaying"] | false;

    _statusCallback(trackName, artistName, albumName, progressMs, durationMs, volumePercent, isPlaying);
}

void MQTTClient::handleImageMessage(uint8_t* payload, unsigned int length) {
    if (_imageCallback) {
        _imageCallback(payload, length);
    }
}

void MQTTClient::handlePlaylistsMessage(uint8_t* payload, unsigned int length) {
    if (!_playlistsCallback) return;

    DynamicJsonDocument doc(8192);
    DeserializationError error = deserializeJson(doc, payload, length);

    if (error) {
        Serial.print("Playlists JSON parse error: ");
        Serial.println(error.c_str());
        return;
    }

    JsonArray playlists = doc.as<JsonArray>();
    _playlistsCallback(playlists);
}

void MQTTClient::handleAlbumsMessage(uint8_t* payload, unsigned int length) {
    if (!_albumsCallback) return;

    DynamicJsonDocument doc(8192);
    DeserializationError error = deserializeJson(doc, payload, length);

    if (error) {
        Serial.print("Albums JSON parse error: ");
        Serial.println(error.c_str());
        return;
    }

    JsonArray albums = doc.as<JsonArray>();
    _albumsCallback(albums);
}

void MQTTClient::sendCommand(const char* command, const char* parameter) {
    StaticJsonDocument<256> doc;
    doc["command"] = command;
    if (parameter) {
        doc["parameter"] = parameter;
    }

    char buffer[256];
    size_t length = serializeJson(doc, buffer);

    if (_mqttClient.publish(MQTT_TOPIC_COMMAND, buffer, length)) {
        Serial.print("Command sent: ");
        Serial.println(command);
    } else {
        Serial.print("Failed to send command: ");
        Serial.println(command);
    }
}

void MQTTClient::play() {
    sendCommand("play");
}

void MQTTClient::pause() {
    sendCommand("pause");
}

void MQTTClient::nextTrack() {
    sendCommand("next");
}

void MQTTClient::previousTrack() {
    sendCommand("previous");
}

void MQTTClient::volumeUp() {
    sendCommand("volume_up");
}

void MQTTClient::volumeDown() {
    sendCommand("volume_down");
}

void MQTTClient::setVolume(int volume) {
    char volumeStr[8];
    snprintf(volumeStr, sizeof(volumeStr), "%d", volume);
    sendCommand("set_volume", volumeStr);
}

void MQTTClient::changePlaylist(const char* playlistId) {
    sendCommand("change_playlist", playlistId);
}

void MQTTClient::changeAlbum(const char* albumId) {
    sendCommand("change_album", albumId);
}

void MQTTClient::getPlaylists() {
    sendCommand("get_playlists");
}

void MQTTClient::getAlbums() {
    sendCommand("get_albums");
}
