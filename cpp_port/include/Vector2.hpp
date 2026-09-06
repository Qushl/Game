#pragma once
#include <cmath>
#include <algorithm>

namespace Game {

struct Vector2 {
    float x = 0.f;
    float y = 0.f;

    Vector2() = default;
    Vector2(float x_, float y_) : x(x_), y(y_) {}

    static Vector2 Zero() { return {0.f, 0.f}; }

    float length() const {
        return std::sqrt(x * x + y * y);
    }

    Vector2 normalized() const {
        float len = length();
        if (len == 0.f) return Zero();
        return {x / len, y / len};
    }

    static Vector2 lerp(const Vector2& a, const Vector2& b, float t) {
        t = std::clamp(t, 0.f, 1.f);
        return {
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t
        };
    }

    Vector2 operator+(const Vector2& other) const {
        return {x + other.x, y + other.y};
    }

    Vector2 operator-(const Vector2& other) const {
        return {x - other.x, y - other.y};
    }

    Vector2 operator*(float scalar) const {
        return {x * scalar, y * scalar};
    }

    Vector2& operator+=(const Vector2& other) {
        x += other.x;
        y += other.y;
        return *this;
    }

    Vector2& operator*=(float scalar) {
        x *= scalar;
        y *= scalar;
        return *this;
    }

    float dot(const Vector2& other) const {
        return x * other.x + y * other.y;
    }

    float angleTo(const Vector2& other) const {
        auto n = other.normalized();
        float d = dot(n) / length();
        d = std::clamp(d, -1.f, 1.f);
        return std::acos(d);
    }
};

}
