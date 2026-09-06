#pragma once
#include <cstdint>

// Simple color representation (RGBA 0-255)
namespace Game {

struct Color {
    uint8_t r = 0, g = 0, b = 0, a = 255;

    static Color fromRgb(uint8_t r, uint8_t g, uint8_t b) {
        return {r, g, b, 255};
    }
};

struct Enemy {
    float worldX = 0.f;
    float worldY = 0.f;
    float distance = 0.f;
    float speed = 0.f;
    float laneOffset = 0.f;
    bool isOncoming = false;
    Color color;
    bool wasRewarded = false;
};

}
