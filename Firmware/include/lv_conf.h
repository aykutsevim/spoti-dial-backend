#ifndef LV_CONF_H
#define LV_CONF_H

#include <stdint.h>

// Color depth: 1 (1 byte per pixel), 8 (RGB332), 16 (RGB565), 32 (ARGB8888)
#define LV_COLOR_DEPTH 16

// Swap the 2 bytes of RGB565 color (useful for displays with different byte order)
#define LV_COLOR_16_SWAP 0

// Memory settings
#define LV_MEM_CUSTOM 0
#define LV_MEM_SIZE (48U * 1024U)  // 48KB for LVGL

// Display settings
#define LV_HOR_RES_MAX 240
#define LV_VER_RES_MAX 240
#define LV_DPI_DEF 130

// Rendering settings
#define LV_DRAW_COMPLEX 1
#define LV_SHADOW_CACHE_SIZE 0
#define LV_CIRCLE_CACHE_SIZE 4
#define LV_LAYER_SIMPLE_BUF_SIZE (24 * 1024)

// Font settings
#define LV_FONT_MONTSERRAT_8 1
#define LV_FONT_MONTSERRAT_10 1
#define LV_FONT_MONTSERRAT_12 1
#define LV_FONT_MONTSERRAT_14 1
#define LV_FONT_MONTSERRAT_16 1
#define LV_FONT_MONTSERRAT_18 1
#define LV_FONT_MONTSERRAT_20 1
#define LV_FONT_MONTSERRAT_22 1
#define LV_FONT_MONTSERRAT_24 1

// Theme settings
#define LV_USE_THEME_DEFAULT 1
#define LV_THEME_DEFAULT_DARK 1

// Widget usage
#define LV_USE_ARC 1
#define LV_USE_BAR 1
#define LV_USE_BTN 1
#define LV_USE_IMG 1
#define LV_USE_LABEL 1
#define LV_USE_LINE 1
#define LV_USE_ROLLER 1
#define LV_USE_SLIDER 1
#define LV_USE_SPINNER 1

// Animation
#define LV_USE_ANIMATION 1

// Logging
#define LV_USE_LOG 1
#define LV_LOG_LEVEL LV_LOG_LEVEL_WARN
#define LV_LOG_PRINTF 1

// Performance monitoring
#define LV_USE_PERF_MONITOR 0
#define LV_USE_MEM_MONITOR 0

// Input device settings
#define LV_INDEV_DEF_READ_PERIOD 30
#define LV_INDEV_DEF_DRAG_LIMIT 10
#define LV_INDEV_DEF_DRAG_THROW 10
#define LV_INDEV_DEF_LONG_PRESS_TIME 400
#define LV_INDEV_DEF_LONG_PRESS_REP_TIME 100

#endif // LV_CONF_H
