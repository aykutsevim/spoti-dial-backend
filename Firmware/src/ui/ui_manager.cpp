#include "ui_manager.h"

UIManager::UIManager()
    : _currentScreen(SCREEN_SPLASH),
      _screen(nullptr),
      _splashScreen(nullptr),
      _nowPlayingScreen(nullptr),
      _playlistsScreen(nullptr),
      _albumsScreen(nullptr),
      _settingsScreen(nullptr),
      _volumeOverlay(nullptr),
      _volumeOverlayTime(0) {
}

bool UIManager::begin() {
    // Create all screens
    createSplashScreen();
    createNowPlayingScreen();
    createPlaylistsScreen();
    createAlbumsScreen();
    createSettingsScreen();

    // Show splash screen initially
    showScreen(SCREEN_SPLASH);

    return true;
}

void UIManager::update() {
    // Handle volume overlay timeout
    if (_volumeOverlay && lv_obj_has_flag(_volumeOverlay, LV_OBJ_FLAG_HIDDEN) == false) {
        if (millis() - _volumeOverlayTime > 2000) {
            hideVolumeOverlay();
        }
    }

    lv_timer_handler();
}

void UIManager::showScreen(UIScreen screen) {
    _currentScreen = screen;

    // Hide all screens
    if (_splashScreen) lv_obj_add_flag(_splashScreen, LV_OBJ_FLAG_HIDDEN);
    if (_nowPlayingScreen) lv_obj_add_flag(_nowPlayingScreen, LV_OBJ_FLAG_HIDDEN);
    if (_playlistsScreen) lv_obj_add_flag(_playlistsScreen, LV_OBJ_FLAG_HIDDEN);
    if (_albumsScreen) lv_obj_add_flag(_albumsScreen, LV_OBJ_FLAG_HIDDEN);
    if (_settingsScreen) lv_obj_add_flag(_settingsScreen, LV_OBJ_FLAG_HIDDEN);

    // Show selected screen
    switch (screen) {
        case SCREEN_SPLASH:
            if (_splashScreen) lv_obj_clear_flag(_splashScreen, LV_OBJ_FLAG_HIDDEN);
            break;
        case SCREEN_NOW_PLAYING:
            if (_nowPlayingScreen) lv_obj_clear_flag(_nowPlayingScreen, LV_OBJ_FLAG_HIDDEN);
            break;
        case SCREEN_PLAYLISTS:
            if (_playlistsScreen) lv_obj_clear_flag(_playlistsScreen, LV_OBJ_FLAG_HIDDEN);
            break;
        case SCREEN_ALBUMS:
            if (_albumsScreen) lv_obj_clear_flag(_albumsScreen, LV_OBJ_FLAG_HIDDEN);
            break;
        case SCREEN_SETTINGS:
            if (_settingsScreen) lv_obj_clear_flag(_settingsScreen, LV_OBJ_FLAG_HIDDEN);
            break;
    }
}

void UIManager::createSplashScreen() {
    _splashScreen = lv_obj_create(lv_scr_act());
    lv_obj_set_size(_splashScreen, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    lv_obj_set_style_bg_color(_splashScreen, lv_color_black(), 0);

    // App name label
    lv_obj_t* nameLabel = lv_label_create(_splashScreen);
    lv_label_set_text(nameLabel, APP_NAME);
    lv_obj_set_style_text_font(nameLabel, &lv_font_montserrat_14, 0);
    lv_obj_set_style_text_color(nameLabel, lv_color_white(), 0);
    lv_obj_align(nameLabel, LV_ALIGN_CENTER, 0, -20);

    // Version label
    lv_obj_t* versionLabel = lv_label_create(_splashScreen);
    lv_label_set_text(versionLabel, "v" APP_VERSION);
    lv_obj_set_style_text_font(versionLabel, &lv_font_montserrat_14, 0);
    lv_obj_set_style_text_color(versionLabel, lv_color_make(128, 128, 128), 0);
    lv_obj_align(versionLabel, LV_ALIGN_CENTER, 0, 10);

    // Loading spinner
    lv_obj_t* spinner = lv_spinner_create(_splashScreen, 1000, 60);
    lv_obj_set_size(spinner, 40, 40);
    lv_obj_align(spinner, LV_ALIGN_CENTER, 0, 50);
}

void UIManager::createNowPlayingScreen() {
    _nowPlayingScreen = lv_obj_create(lv_scr_act());
    lv_obj_set_size(_nowPlayingScreen, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    lv_obj_set_style_bg_color(_nowPlayingScreen, lv_color_black(), 0);

    // Album art (centered, circular)
    _albumArtImage = lv_img_create(_nowPlayingScreen);
    lv_obj_set_size(_albumArtImage, 120, 120);
    lv_obj_align(_albumArtImage, LV_ALIGN_CENTER, 0, -40);

    // Track name
    _trackLabel = lv_label_create(_nowPlayingScreen);
    lv_label_set_text(_trackLabel, "No track");
    lv_obj_set_style_text_font(_trackLabel, &lv_font_montserrat_14, 0);
    lv_obj_set_style_text_color(_trackLabel, lv_color_white(), 0);
    lv_obj_align(_trackLabel, LV_ALIGN_CENTER, 0, 50);
    lv_label_set_long_mode(_trackLabel, LV_LABEL_LONG_SCROLL_CIRCULAR);
    lv_obj_set_width(_trackLabel, DISPLAY_WIDTH - 20);

    // Artist name
    _artistLabel = lv_label_create(_nowPlayingScreen);
    lv_label_set_text(_artistLabel, "No artist");
    lv_obj_set_style_text_font(_artistLabel, &lv_font_montserrat_14, 0);
    lv_obj_set_style_text_color(_artistLabel, lv_color_make(180, 180, 180), 0);
    lv_obj_align(_artistLabel, LV_ALIGN_CENTER, 0, 70);
    lv_label_set_long_mode(_artistLabel, LV_LABEL_LONG_SCROLL_CIRCULAR);
    lv_obj_set_width(_artistLabel, DISPLAY_WIDTH - 20);

    // Progress bar
    _progressBar = lv_bar_create(_nowPlayingScreen);
    lv_obj_set_size(_progressBar, DISPLAY_WIDTH - 40, 4);
    lv_obj_align(_progressBar, LV_ALIGN_BOTTOM_MID, 0, -30);
    lv_bar_set_value(_progressBar, 0, LV_ANIM_OFF);

    // Progress label (time)
    _progressLabel = lv_label_create(_nowPlayingScreen);
    lv_label_set_text(_progressLabel, "0:00 / 0:00");
    lv_obj_set_style_text_font(_progressLabel, &lv_font_montserrat_14, 0);
    lv_obj_set_style_text_color(_progressLabel, lv_color_make(128, 128, 128), 0);
    lv_obj_align(_progressLabel, LV_ALIGN_BOTTOM_MID, 0, -10);

    // Play/Pause icon
    _playPauseIcon = lv_label_create(_nowPlayingScreen);
    lv_label_set_text(_playPauseIcon, LV_SYMBOL_PLAY);
    lv_obj_set_style_text_font(_playPauseIcon, &lv_font_montserrat_14, 0);
    lv_obj_align(_playPauseIcon, LV_ALIGN_TOP_MID, 0, 10);
}

void UIManager::createPlaylistsScreen() {
    _playlistsScreen = lv_obj_create(lv_scr_act());
    lv_obj_set_size(_playlistsScreen, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    lv_obj_set_style_bg_color(_playlistsScreen, lv_color_black(), 0);

    // Title
    lv_obj_t* titleLabel = lv_label_create(_playlistsScreen);
    lv_label_set_text(titleLabel, "Playlists");
    lv_obj_set_style_text_font(titleLabel, &lv_font_montserrat_14, 0);
    lv_obj_align(titleLabel, LV_ALIGN_TOP_MID, 0, 10);

    // Roller for playlist list
    _listRoller = lv_roller_create(_playlistsScreen);
    lv_obj_set_size(_listRoller, DISPLAY_WIDTH - 40, 150);
    lv_obj_align(_listRoller, LV_ALIGN_CENTER, 0, 20);
    lv_roller_set_options(_listRoller, "Loading...", LV_ROLLER_MODE_NORMAL);
}

void UIManager::createAlbumsScreen() {
    _albumsScreen = lv_obj_create(lv_scr_act());
    lv_obj_set_size(_albumsScreen, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    lv_obj_set_style_bg_color(_albumsScreen, lv_color_black(), 0);

    // Similar to playlists screen
    lv_obj_t* titleLabel = lv_label_create(_albumsScreen);
    lv_label_set_text(titleLabel, "Albums");
    lv_obj_set_style_text_font(titleLabel, &lv_font_montserrat_14, 0);
    lv_obj_align(titleLabel, LV_ALIGN_TOP_MID, 0, 10);
}

void UIManager::createSettingsScreen() {
    _settingsScreen = lv_obj_create(lv_scr_act());
    lv_obj_set_size(_settingsScreen, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    lv_obj_set_style_bg_color(_settingsScreen, lv_color_black(), 0);

    lv_obj_t* titleLabel = lv_label_create(_settingsScreen);
    lv_label_set_text(titleLabel, "Settings");
    lv_obj_set_style_text_font(titleLabel, &lv_font_montserrat_14, 0);
    lv_obj_align(titleLabel, LV_ALIGN_TOP_MID, 0, 10);
}

void UIManager::updateNowPlaying(const char* trackName, const char* artistName,
                                 const char* albumName, int progressMs, int durationMs,
                                 int volumePercent, bool isPlaying) {
    if (_trackLabel) {
        lv_label_set_text(_trackLabel, trackName);
    }

    if (_artistLabel) {
        lv_label_set_text(_artistLabel, artistName);
    }

    if (_progressBar && durationMs > 0) {
        int percent = (progressMs * 100) / durationMs;
        lv_bar_set_value(_progressBar, percent, LV_ANIM_OFF);
    }

    if (_progressLabel) {
        char buffer[32];
        char currentTime[16], totalTime[16];
        formatTime(progressMs, currentTime, sizeof(currentTime));
        formatTime(durationMs, totalTime, sizeof(totalTime));
        snprintf(buffer, sizeof(buffer), "%s / %s", currentTime, totalTime);
        lv_label_set_text(_progressLabel, buffer);
    }

    if (_playPauseIcon) {
        lv_label_set_text(_playPauseIcon, isPlaying ? LV_SYMBOL_PAUSE : LV_SYMBOL_PLAY);
    }
}

void UIManager::showVolumeOverlay(int volume) {
    if (!_volumeOverlay) {
        // Create volume overlay
        _volumeOverlay = lv_obj_create(lv_scr_act());
        lv_obj_set_size(_volumeOverlay, 100, 100);
        lv_obj_align(_volumeOverlay, LV_ALIGN_CENTER, 0, 0);
        lv_obj_set_style_bg_color(_volumeOverlay, lv_color_make(40, 40, 40), 0);
        lv_obj_set_style_bg_opa(_volumeOverlay, LV_OPA_90, 0);
        lv_obj_set_style_radius(_volumeOverlay, 10, 0);

        _volumeLabel = lv_label_create(_volumeOverlay);
        lv_obj_set_style_text_font(_volumeLabel, &lv_font_montserrat_14, 0);
        lv_obj_align(_volumeLabel, LV_ALIGN_CENTER, 0, 0);
    }

    char buffer[16];
    snprintf(buffer, sizeof(buffer), "%d%%", volume);
    lv_label_set_text(_volumeLabel, buffer);
    lv_obj_clear_flag(_volumeOverlay, LV_OBJ_FLAG_HIDDEN);
    _volumeOverlayTime = millis();
}

void UIManager::hideVolumeOverlay() {
    if (_volumeOverlay) {
        lv_obj_add_flag(_volumeOverlay, LV_OBJ_FLAG_HIDDEN);
    }
}

void UIManager::formatTime(int milliseconds, char* buffer, size_t bufferSize) {
    int totalSeconds = milliseconds / 1000;
    int minutes = totalSeconds / 60;
    int seconds = totalSeconds % 60;
    snprintf(buffer, bufferSize, "%d:%02d", minutes, seconds);
}

void UIManager::onEncoderChange(int delta) {
    // Handle encoder rotation based on current screen
    // To be implemented based on UI requirements
}

void UIManager::onEncoderClick() {
    // Handle encoder click based on current screen
    // To be implemented based on UI requirements
}

void UIManager::updateAlbumArt(uint8_t* imageData, size_t length) {
    // To be implemented - decode and display album art
}

void UIManager::updatePlaylists(const char** playlists, size_t count) {
    // To be implemented
}

void UIManager::updateAlbums(const char** albums, size_t count) {
    // To be implemented
}
