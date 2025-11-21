#include "lv_display.h"
#include <M5Dial.h>
#include <lvgl.h>

// LVGL display buffer
static lv_disp_draw_buf_t draw_buf;
static lv_color_t buf[DISPLAY_WIDTH * DISPLAY_HEIGHT / 10];

// LVGL display driver callback
static void display_flush_cb(lv_disp_drv_t* disp_drv, const lv_area_t* area,
                             lv_color_t* color_p) {
    uint32_t w = (area->x2 - area->x1 + 1);
    uint32_t h = (area->y2 - area->y1 + 1);

    // Get M5Dial display instance
    auto& display = M5.Lcd;

    // Set the drawing area
    display.startWrite();
    display.setAddrWindow(area->x1, area->y1, w, h);

    // Write pixel data
    uint32_t pixel_count = w * h;
    for (uint32_t i = 0; i < pixel_count; i++) {
        // Convert LVGL color (16-bit) to M5GFX format
        uint16_t color = (color_p[i].full);
        display.pushColor(color, 1);
    }
    display.endWrite();

    // Flush complete
    lv_disp_flush_ready(disp_drv);
}

bool lv_display_init() {
    // Initialize LVGL
    lv_init();

    // Create display buffer for partial redraws
    lv_disp_draw_buf_init(&draw_buf, buf, NULL, DISPLAY_WIDTH * DISPLAY_HEIGHT / 10);

    // Create display driver
    static lv_disp_drv_t disp_drv;
    lv_disp_drv_init(&disp_drv);

    // Set display driver parameters
    disp_drv.draw_buf = &draw_buf;
    disp_drv.flush_cb = display_flush_cb;
    disp_drv.hor_res = DISPLAY_WIDTH;
    disp_drv.ver_res = DISPLAY_HEIGHT;

    // Register display driver
    lv_disp_drv_register(&disp_drv);

    return true;
}
