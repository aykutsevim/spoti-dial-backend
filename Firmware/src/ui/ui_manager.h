#ifndef UI_MANAGER_H
#define UI_MANAGER_H

#include <M5Dial.h>
#include <lvgl.h>
#include "config.h"

// UI Screens
enum UIScreen {
    SCREEN_SPLASH,
    SCREEN_NOW_PLAYING,
    SCREEN_PLAYLISTS,
    SCREEN_ALBUMS,
    SCREEN_SETTINGS
};

class UIManager {
public:
    UIManager();

    // Initialization
    bool begin();
    void update();

    // Screen management
    void showScreen(UIScreen screen);
    UIScreen getCurrentScreen() { return _currentScreen; }

    // Now Playing screen updates
    void updateNowPlaying(const char* trackName, const char* artistName,
                         const char* albumName, int progressMs, int durationMs,
                         int volumePercent, bool isPlaying);
    void updateAlbumArt(uint8_t* imageData, size_t length);

    // Playlist/Album list updates
    void updatePlaylists(const char** playlists, size_t count);
    void updateAlbums(const char** albums, size_t count);

    // Encoder handling
    void onEncoderChange(int delta);
    void onEncoderClick();

    // Volume control
    void showVolumeOverlay(int volume);
    void hideVolumeOverlay();

private:
    UIScreen _currentScreen;
    lv_obj_t* _screen;

    // Screen objects
    lv_obj_t* _splashScreen;
    lv_obj_t* _nowPlayingScreen;
    lv_obj_t* _playlistsScreen;
    lv_obj_t* _albumsScreen;
    lv_obj_t* _settingsScreen;

    // Now Playing widgets
    lv_obj_t* _trackLabel;
    lv_obj_t* _artistLabel;
    lv_obj_t* _albumLabel;
    lv_obj_t* _progressBar;
    lv_obj_t* _progressLabel;
    lv_obj_t* _albumArtImage;
    lv_obj_t* _playPauseIcon;
    lv_obj_t* _volumeArc;

    // Volume overlay
    lv_obj_t* _volumeOverlay;
    lv_obj_t* _volumeLabel;
    unsigned long _volumeOverlayTime;

    // List widgets
    lv_obj_t* _listRoller;

    // Screen creation
    void createSplashScreen();
    void createNowPlayingScreen();
    void createPlaylistsScreen();
    void createAlbumsScreen();
    void createSettingsScreen();

    // Helper functions
    void formatTime(int milliseconds, char* buffer, size_t bufferSize);
};

#endif // UI_MANAGER_H
