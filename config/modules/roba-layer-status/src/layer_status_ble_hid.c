/*
 * Embeds active layer number in keyboard HID report's reserved byte,
 * and also holds F14–F18 so Windows Raw Input can detect layer changes
 * when BLE HID open is blocked by the OS.
 *
 * Based on maatthc/zmk-feature-appcompanion (MIT), adapted for ZMK main.
 */

#include <zephyr/logging/log.h>

#include <dt-bindings/zmk/hid_usage_pages.h>
#include <zmk/endpoints.h>
#include <zmk/event_manager.h>
#include <zmk/events/layer_state_changed.h>
#include <zmk/hid.h>
#include <zmk/keymap.h>

LOG_MODULE_DECLARE(zmk, CONFIG_ZMK_LOG_LEVEL);

/* HID keyboard usages: F13=0x68 … F18=0x6D. Layer N (1–5) → F(13+N). */
#define LAYER_INDICATOR_F13 0x68
#define LAYER_INDICATOR_MAX 5

static uint8_t current_layer = 0;
static uint8_t indicator_usage = 0;

static void update_layer_indicator_key(uint8_t layer) {
    if (indicator_usage != 0) {
        zmk_hid_keyboard_release(indicator_usage);
        indicator_usage = 0;
    }

    if (layer >= 1 && layer <= LAYER_INDICATOR_MAX) {
        indicator_usage = LAYER_INDICATOR_F13 + layer; /* layer1→F14 … layer5→F18 */
        zmk_hid_keyboard_press(indicator_usage);
    }
}

static int layer_status_embedded_listener(const zmk_event_t *eh) {
    const struct zmk_layer_state_changed *ev = as_zmk_layer_state_changed(eh);
    if (ev == NULL) {
        return -ENOTSUP;
    }

    uint8_t layer = zmk_keymap_highest_layer_active();

    if (layer == current_layer) {
        return 0;
    }
    current_layer = layer;

    struct zmk_hid_keyboard_report *report = zmk_hid_get_keyboard_report();
    report->body._reserved = layer;

    update_layer_indicator_key(layer);

    zmk_endpoint_send_report(HID_USAGE_KEY);

    return 0;
}

ZMK_LISTENER(layer_status_embedded, layer_status_embedded_listener);
ZMK_SUBSCRIPTION(layer_status_embedded, zmk_layer_state_changed);
