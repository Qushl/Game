#pragma once
#include "PlayerCar.hpp"
#include "RoadPath.hpp"
#include "Enemy.hpp"
#include <vector>
#include <cmath>
#include <algorithm>
#include <random>
#include <mutex>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

class GameManager {
public:
    static constexpr int LanesPerDirection = 4;
    static constexpr int LaneCount = LanesPerDirection * 2;
    static constexpr float LaneWidth = 72.f;
    static constexpr float RoadWidth = LaneCount * LaneWidth;
    static constexpr float ScreenWidth = 800.f;
    static constexpr float ScreenHeight = 800.f;

    GameManager(int screenWidth = 800, int /*screenHeight*/ = 800) {
        player_.setWorldX(static_cast<float>(screenWidth) / 2.f);
        player_.setWorldY(0.f);
        player_.setAngle(-static_cast<float>(M_PI) / 2.f);
        setPlayerModel(currentModel_);
    }

    void update() {
        if (gameOver_ || updateInProgress_) return;
        updateGameLogic();
    }

    void setPlayerModel(PlayerCar::CarModelType model) {
        currentModel_ = model;
        player_.setModel(model);
    }

    void reset() {
        player_ = PlayerCar();
        setPlayerModel(currentModel_);
        player_.setWorldX(400.f);
        player_.setWorldY(-500.f);
        enemies_.clear();
        score_ = 0;
        gameOver_ = false;
        spawnRate_ = 50;
        spawnCounter_ = 0;
    }

    // Public accessors
    PlayerCar& player() { return player_; }
    const PlayerCar& player() const { return player_; }

    const std::vector<Game::Enemy>& enemies() const { return enemies_; }
    int score() const { return score_; }
    bool isGameOver() const { return gameOver_; }
    int maxEnemies() const { return maxEnemies_; }
    void setMaxEnemies(int v) { maxEnemies_ = v; }

    RoadPath& roadPath() { return roadPath_; }
    const RoadPath& roadPath() const { return roadPath_; }

    float cameraAngle() const { return cameraAngle_; }

private:
    PlayerCar player_;
    std::vector<Game::Enemy> enemies_;
    RoadPath roadPath_;

    float cameraAngle_ = 0.f;
    static constexpr float CameraTurnLerp = 0.04f;

    int spawnCounter_ = 0;
    int spawnRate_ = 50;
    std::mt19937 rng_{std::random_device{}()};

    int score_ = 0;
    bool gameOver_ = false;

    int maxEnemies_ = 12;
    PlayerCar::CarModelType currentModel_ = PlayerCar::CarModelType::Default;

    std::mutex lockObject_;
    bool updateInProgress_ = false;

    float randFloat() {
        std::uniform_real_distribution<float> dist(0.f, 1.f);
        return dist(rng_);
    }

    int randInt(int min, int max) {
        std::uniform_int_distribution<int> dist(min, max);
        return dist(rng_);
    }

    void updateGameLogic() {
        updateInProgress_ = true;
        try {
            std::lock_guard<std::mutex> lock(lockObject_);
            if (gameOver_) return;

            player_.setIsOnRoad(isPlayerOnRoad());
            player_.update(0.f);
            checkCollisions();

            ++spawnCounter_;
            if (spawnCounter_ > spawnRate_) {
                spawnEnemy();
                spawnCounter_ = 0;
                spawnRate_ = std::max(20, spawnRate_ - 1);
            }

            float playerDistanceOnRoad = -player_.worldY();

            // Update enemies
            for (auto& enemy : enemies_) {
                if (enemy.isOncoming) {
                    enemy.distance -= enemy.speed;
                } else {
                    enemy.distance += enemy.speed;
                }
                updateEnemyWorldPosition(enemy);
            }

            // Remove far enemies + scoring
            enemies_.erase(
                std::remove_if(enemies_.begin(), enemies_.end(),
                    [&](Game::Enemy& enemy) {
                        float dx = enemy.worldX - player_.worldX();
                        float dy = enemy.worldY - player_.worldY();
                        float distToPlayer = std::sqrt(dx * dx + dy * dy);
                        constexpr float passDistance = 80.f;
                        if (distToPlayer < passDistance && !enemy.wasRewarded) {
                            score_ += 600;
                            enemy.wasRewarded = true;
                        }
                        return enemy.distance < playerDistanceOnRoad - 3000.f;
                    }),
                enemies_.end()
            );

            if (player_.isOnRoad()) {
                ++score_;
            } else if (score_ > 0) {
                score_ -= 2;
            }

            spawnRate_ = std::max(35, 60 - score_ / 1000);
        } catch (...) {
            // swallow
        }
        updateInProgress_ = false;
    }

    void spawnEnemy() {
        if (static_cast<int>(enemies_.size()) >= maxEnemies_) return;

        float playerDistanceOnRoad = -player_.worldY();
        float spawnDistAhead = playerDistanceOnRoad + 900.f;

        float spawnX, spawnY, angleF;
        roadPath_.getWorldPosition(spawnDistAhead, spawnX, spawnY, angleF);

        static const Game::Color enemyColors[] = {
            Game::Color::fromRgb(255, 0, 0),     // Red
            Game::Color::fromRgb(0, 128, 0),     // Green
            Game::Color::fromRgb(128, 0, 128),   // Purple
            Game::Color::fromRgb(255, 165, 0),   // Orange
            Game::Color::fromRgb(0, 255, 255),   // Cyan
            Game::Color::fromRgb(255, 255, 0)    // Yellow
        };

        // Forward enemies (same direction)
        int enemiesForward = randInt(LanesPerDirection - 1, LanesPerDirection);
        std::vector<int> usedLanesF;
        for (int i = 0; i < enemiesForward; ++i) {
            if (static_cast<int>(enemies_.size()) >= maxEnemies_) break;

            int lane;
            do {
                lane = randInt(LanesPerDirection, LaneCount - 1);
            } while (std::find(usedLanesF.begin(), usedLanesF.end(), lane) != usedLanesF.end());
            usedLanesF.push_back(lane);

            float laneOffset = -RoadWidth / 2.f + LaneWidth / 2.f + lane * LaneWidth;

            bool occupied = false;
            for (const auto& e : enemies_) {
                if (!e.isOncoming &&
                    std::abs(e.laneOffset - laneOffset) < 1.f &&
                    std::abs(e.distance - spawnDistAhead) < LaneWidth) {
                    occupied = true;
                    break;
                }
            }
            if (occupied) continue;

            Game::Enemy enemy;
            enemy.distance = spawnDistAhead;
            enemy.speed = 0.8f + randFloat() * 0.8f;
            enemy.laneOffset = laneOffset;
            enemy.isOncoming = false;
            enemy.color = enemyColors[randInt(0, 5)];
            updateEnemyWorldPosition(enemy);
            enemies_.push_back(enemy);
        }

        // Oncoming enemies
        int enemiesBackward = randInt(LanesPerDirection - 1, LanesPerDirection);
        std::vector<int> usedLanesB;
        for (int i = 0; i < enemiesBackward; ++i) {
            if (static_cast<int>(enemies_.size()) >= maxEnemies_) break;

            int lane;
            do {
                lane = randInt(0, LanesPerDirection - 1);
            } while (std::find(usedLanesB.begin(), usedLanesB.end(), lane) != usedLanesB.end());
            usedLanesB.push_back(lane);

            float laneOffset = -RoadWidth / 2.f + LaneWidth / 2.f + lane * LaneWidth;

            bool occupied = false;
            for (const auto& e : enemies_) {
                if (e.isOncoming &&
                    std::abs(e.laneOffset - laneOffset) < 1.f &&
                    std::abs(e.distance - spawnDistAhead) < LaneWidth) {
                    occupied = true;
                    break;
                }
            }
            if (occupied) continue;

            Game::Enemy enemy;
            enemy.distance = spawnDistAhead;
            enemy.speed = 0.8f + randFloat() * 0.8f;
            enemy.laneOffset = laneOffset;
            enemy.isOncoming = true;
            enemy.color = enemyColors[randInt(0, 5)];
            updateEnemyWorldPosition(enemy);
            enemies_.push_back(enemy);
        }
    }

    void checkCollisions() {
        float collisionRadius = std::max(30.f, std::max(player_.width(), player_.height()) * 0.6f);
        for (const auto& enemy : enemies_) {
            float dx = player_.worldX() - enemy.worldX;
            float dy = player_.worldY() - enemy.worldY;
            float distance = std::sqrt(dx * dx + dy * dy);
            if (distance < collisionRadius) {
                gameOver_ = true;
                break;
            }
        }
    }

    void updateEnemyWorldPosition(Game::Enemy& enemy) {
        float x, y, angle;
        roadPath_.getWorldPosition(enemy.distance, x, y, angle);
        float perpX = std::cos(angle);
        float perpY = std::sin(angle);
        enemy.worldX = x + perpX * enemy.laneOffset;
        enemy.worldY = y + perpY * enemy.laneOffset;
    }

    bool isPlayerOnRoad() const {
        float playerDistanceOnRoad = -player_.worldY();
        float roadCenterX, roadCenterY, roadAngle;
        roadPath_.getWorldPosition(playerDistanceOnRoad, roadCenterX, roadCenterY, roadAngle);

        float dx = player_.worldX() - roadCenterX;
        float dy = player_.worldY() - roadCenterY;
        float perpX = std::cos(roadAngle);
        float perpY = std::sin(roadAngle);
        float lateralDistance = std::abs(dx * perpX + dy * perpY);
        constexpr float roadWidthHalf = RoadWidth / 2.f;
        return lateralDistance < roadWidthHalf;
    }

    // Camera helpers (for future renderer)
public:
    float getCameraTargetAngle() const {
        float speed = player_.velocity().length();
        if (speed > 0.3f) {
            float heading = std::atan2(player_.velocity().y, player_.velocity().x);
            return -heading - static_cast<float>(M_PI) / 2.f;
        }
        return -player_.angle() - static_cast<float>(M_PI) / 2.f;
    }

    static float lerpAngle(float current, float target, float t) {
        float diff = target - current;
        while (diff >  static_cast<float>(M_PI)) diff -= 2.f * static_cast<float>(M_PI);
        while (diff < -static_cast<float>(M_PI)) diff += 2.f * static_cast<float>(M_PI);
        return current + diff * t;
    }

    void updateCamera() {
        float target = getCameraTargetAngle();
        cameraAngle_ = lerpAngle(cameraAngle_, target, CameraTurnLerp);
    }
};
