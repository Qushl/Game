#pragma once
#include "Vector2.hpp"
#include "Enemy.hpp" // for Color
#include <vector>
#include <cmath>
#include <algorithm>
#include <random>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

struct DriftSmoke {
    float x = 0.f;
    float y = 0.f;
    float life = 0.f;
    float size = 0.f;
    float velocityX = 0.f;
    float velocityY = 0.f;
    bool isGrass = false;
};

class PlayerCar {
public:
    enum class CarModelType {
        Default,
        Straight,
        Sideways
    };

    PlayerCar() {
        setModel(model_);
        // Pre-create some particles like in original
        for (int i = 0; i < 10; ++i) {
            DriftSmoke s;
            s.x = worldX_;
            s.y = worldY_ + i * (height_ * 0.12f);
            s.life = 1.0f - i * 0.08f;
            s.size = 12.f + i * 3.f;
            smokeParticles_.push_back(s);
        }
    }

    void setModel(CarModelType model) {
        model_ = model;
        switch (model) {
            case CarModelType::Default:
                width_ = 32.f;
                height_ = 48.f;
                color_ = Game::Color::fromRgb(100, 149, 237); // CornflowerBlue
                turnSpeed_ = 0.09f;
                acceleration_ = 0.30f;
                maxSpeed_ = 10.f;
                friction_ = 0.97f;
                driftFactor_ = 0.92f;
                break;
            case CarModelType::Straight:
                width_ = 30.f;
                height_ = 46.f;
                color_ = Game::Color::fromRgb(176, 196, 222); // LightSteelBlue
                turnSpeed_ = 0.095f;
                acceleration_ = 0.34f;
                maxSpeed_ = 11.5f;
                friction_ = 0.975f;
                driftFactor_ = 0.6f;
                break;
            case CarModelType::Sideways:
                width_ = 32.f;
                height_ = 48.f;
                color_ = Game::Color::fromRgb(147, 112, 219); // MediumPurple
                turnSpeed_ = 0.09f;
                acceleration_ = 0.30f;
                maxSpeed_ = 10.f;
                friction_ = 0.97f;
                driftFactor_ = 0.985f;
                break;
        }
    }

    void setInput(bool left, bool right, bool accelerate, bool brake = false) {
        turningLeft_ = left;
        turningRight_ = right;
        isAccelerating_ = accelerate;
        isBraking_ = brake;
    }

    void update(float /*scrollSpeed*/ = 0.f) {
        float effectiveTurnSpeed = turnSpeed_;
        float effectiveFriction = friction_;

        if (!isOnRoad_) {
            effectiveTurnSpeed = turnSpeed_ * 0.35f;
            effectiveFriction = 0.93f;

            if (wasOnRoadLastFrame_) {
                float randomAngle = (randFloat() - 0.5f) * 0.9f;
                angle_ += randomAngle;
            }
        }

        wasOnRoadLastFrame_ = isOnRoad_;

        if (turningLeft_)  angle_ -= effectiveTurnSpeed;
        if (turningRight_) angle_ += effectiveTurnSpeed;

        // Normalize angle to [-PI, PI]
        while (angle_ >  static_cast<float>(M_PI)) angle_ -= 2.f * static_cast<float>(M_PI);
        while (angle_ < -static_cast<float>(M_PI)) angle_ += 2.f * static_cast<float>(M_PI);

        Game::Vector2 forward{std::cos(angle_), std::sin(angle_)};

        if (isAccelerating_) {
            velocity_ = velocity_ + (forward * acceleration_);
        }
        if (isBraking_) {
            velocity_ = velocity_ - (forward * (acceleration_ * 0.5f));
        }

        float speed = velocity_.length();
        if (speed > maxSpeed_) {
            velocity_ = velocity_.normalized() * maxSpeed_;
        }

        float effectiveDriftFactor = driftFactor_;
        if (!isOnRoad_) effectiveDriftFactor = 0.99f;

        if (speed > 0.5f) {
            Game::Vector2 desiredDirection = forward * speed;
            float lerpWeight;

            switch (model_) {
                case CarModelType::Straight:
                    lerpWeight = std::clamp(1.f - effectiveDriftFactor + 0.35f, 0.05f, 0.98f);
                    break;
                case CarModelType::Sideways:
                    lerpWeight = std::clamp(1.f - effectiveDriftFactor + 0.05f, 0.02f, 0.7f);
                    break;
                default:
                    lerpWeight = std::clamp(1.f - effectiveDriftFactor, 0.02f, 0.6f);
                    break;
            }

            velocity_ = Game::Vector2::lerp(velocity_, desiredDirection, lerpWeight);
        }

        velocity_ = velocity_ * effectiveFriction;

        // Drift intensity
        if (speed > 0.1f) {
            Game::Vector2 normalizedVelocity = velocity_.normalized();
            float dot = normalizedVelocity.dot(forward);
            driftIntensity_ = std::max(0.f, 1.f - std::abs(dot)) * speed / maxSpeed_;
        } else {
            driftIntensity_ = 0.f;
        }

        // Smoke particles
        if (isOnRoad_ && driftIntensity_ > 0.05f && speed > 0.5f) {
            int smokeCount = static_cast<int>(1 + driftIntensity_ * 3);
            for (int i = 0; i < smokeCount; ++i) {
                DriftSmoke smoke;
                smoke.x = worldX_ - forward.x * (height_ * 0.36f) + (randFloat() - 0.5f) * (width_ * 0.5f);
                smoke.y = worldY_ - forward.y * (height_ * 0.36f) + (randFloat() - 0.5f) * (width_ * 0.5f);
                smoke.life = 1.8f;
                smoke.size = std::max(12.f, height_ * 0.32f + driftIntensity_ * 28.f);
                smoke.velocityX = (randFloat() - 0.5f) * 2.5f;
                smoke.velocityY = (randFloat() - 0.5f) * 2.5f;
                smoke.isGrass = false;
                smokeParticles_.push_back(smoke);
            }
        }

        if (!isOnRoad_ && speed > 0.5f) {
            DriftSmoke grassSmoke;
            grassSmoke.x = worldX_ - forward.x * (height_ * 0.36f) + (randFloat() - 0.5f) * (width_ * 0.6f);
            grassSmoke.y = worldY_ - forward.y * (height_ * 0.36f) + (randFloat() - 0.5f) * (width_ * 0.6f);
            grassSmoke.life = 1.2f;
            grassSmoke.size = std::max(10.f, height_ * 0.22f + randFloat() * 12.f);
            grassSmoke.velocityX = (randFloat() - 0.5f) * 3.0f;
            grassSmoke.velocityY = (randFloat() - 0.5f) * 3.0f;
            grassSmoke.isGrass = true;
            smokeParticles_.push_back(grassSmoke);
        }

        // Update particles
        for (int i = static_cast<int>(smokeParticles_.size()) - 1; i >= 0; --i) {
            auto& p = smokeParticles_[i];
            p.life -= 0.03f;
            p.x += p.velocityX;
            p.y += p.velocityY;
            p.size += 0.8f;
            if (p.life <= 0.f) {
                smokeParticles_.erase(smokeParticles_.begin() + i);
            }
        }

        while (smokeParticles_.size() > 250) {
            smokeParticles_.erase(smokeParticles_.begin());
        }

        worldX_ += velocity_.x;
        worldY_ += velocity_.y;
    }

    // Getters / Setters
    float worldX() const { return worldX_; }
    float worldY() const { return worldY_; }
    void setWorldX(float v) { worldX_ = v; }
    void setWorldY(float v) { worldY_ = v; }

    float angle() const { return angle_; }
    void setAngle(float a) { angle_ = a; }

    const Game::Vector2& velocity() const { return velocity_; }
    float driftIntensity() const { return driftIntensity_; }
    bool isOnRoad() const { return isOnRoad_; }
    void setIsOnRoad(bool v) { isOnRoad_ = v; }

    float width() const { return width_; }
    float height() const { return height_; }
    Game::Color color() const { return color_; }
    CarModelType model() const { return model_; }

    const std::vector<DriftSmoke>& smokeParticles() const { return smokeParticles_; }

    bool isBraking() const { return isBraking_; }

private:
    CarModelType model_ = CarModelType::Default;

    float worldX_ = 400.f;
    float worldY_ = 400.f;
    float angle_ = -static_cast<float>(M_PI) / 2.f;
    Game::Vector2 velocity_;

    float turnSpeed_ = 0.08f;
    float acceleration_ = 0.3f;
    float maxSpeed_ = 10.f;
    float friction_ = 0.97f;
    float driftFactor_ = 0.97f;
    float driftIntensity_ = 0.f;

    float width_ = 32.f;
    float height_ = 48.f;
    Game::Color color_;

    bool isOnRoad_ = true;
    bool wasOnRoadLastFrame_ = true;

    bool turningLeft_ = false;
    bool turningRight_ = false;
    bool isAccelerating_ = false;
    bool isBraking_ = false;

    std::vector<DriftSmoke> smokeParticles_;
    std::mt19937 rng_{std::random_device{}()};

    float randFloat() {
        std::uniform_real_distribution<float> dist(0.f, 1.f);
        return dist(rng_);
    }
};
