#include "GameManager.hpp"
#include "raylib.h"
#include <cmath>
#include <algorithm>
#include <string>

// Convert our Color to Raylib Color
Color ToRayColor(const Game::Color& c) {
    return { c.r, c.g, c.b, c.a };
}

int main() {
    const int screenWidth = 800;
    const int screenHeight = 800;

    SetConfigFlags(FLAG_MSAA_4X_HINT | FLAG_WINDOW_RESIZABLE);
    InitWindow(screenWidth, screenHeight, "Top-Down Highway Drifter (C++)");
    SetTargetFPS(60);

    GameManager game(screenWidth, screenHeight);

    // Camera
    Camera2D camera{};
    camera.target = { game.player().worldX(), game.player().worldY() };
    camera.offset = { screenWidth / 2.0f, screenHeight / 2.0f };
    camera.rotation = 0.0f;
    camera.zoom = 1.0f;

    bool keyLeft = false, keyRight = false, keyUp = false, keyDown = false;

    while (!WindowShouldClose()) {
        // --- Input ---
        keyLeft  = IsKeyDown(KEY_A) || IsKeyDown(KEY_LEFT);
        keyRight = IsKeyDown(KEY_D) || IsKeyDown(KEY_RIGHT);
        keyUp    = IsKeyDown(KEY_W) || IsKeyDown(KEY_UP);
        keyDown  = IsKeyDown(KEY_S) || IsKeyDown(KEY_DOWN);

        if (IsKeyPressed(KEY_R) && game.isGameOver()) {
            game.reset();
        }

        if (IsKeyPressed(KEY_ESCAPE)) break;

        // --- Update logic ---
        game.player().setInput(keyLeft, keyRight, keyUp, keyDown);
        game.update();
        game.updateCamera();

        // --- Update camera ---
        camera.target = { game.player().worldX(), game.player().worldY() };
        // Raylib rotation is in degrees, clockwise positive. Our angle is radians.
        float camAngleDeg = game.cameraAngle() * (180.0f / PI);
        camera.rotation = camAngleDeg;

        // --- Draw ---
        BeginDrawing();
        ClearBackground(DARKGRAY);

        BeginMode2D(camera);

        // Draw road (simplified but good looking)
        {
            const float visibleDist = 2200.0f;
            float playerDist = -game.player().worldY();
            float startDist = playerDist - visibleDist;
            float endDist   = playerDist + visibleDist;

            // Ensure road is generated far enough
            float curDist = game.roadPath().totalLength();
            float curAngle = 0.f;
            if (!game.roadPath().segments().empty()) {
                curAngle = game.roadPath().segments().back().endAngle;
            }
            while (game.roadPath().totalLength() < endDist + 1500.f) {
                game.roadPath().addSegment(curDist, curAngle);
            }

            const float step = 28.0f;
            const int laneCount = GameManager::LaneCount;
            const float laneW = GameManager::LaneWidth;

            // Collect points for left and right edges
            std::vector<Vector2> leftEdge, rightEdge;
            std::vector<std::vector<Vector2>> laneLines(laneCount + 1);

            for (float d = startDist; d < endDist; d += step) {
                float x1, y1, a1;
                game.roadPath().getWorldPosition(d, x1, y1, a1);
                float x2, y2, a2;
                game.roadPath().getWorldPosition(d + step, x2, y2, a2);

                float dx = x2 - x1;
                float dy = y2 - y1;
                float len = std::sqrt(dx * dx + dy * dy);
                if (len < 0.1f) continue;
                dx /= len; dy /= len;
                float perpX = -dy;
                float perpY = dx;

                for (int lane = 0; lane <= laneCount; ++lane) {
                    float offset = (lane - laneCount / 2.0f) * laneW;
                    Vector2 p1 = { x1 + perpX * offset, y1 + perpY * offset };
                    Vector2 p2 = { x2 + perpX * offset, y2 + perpY * offset };
                    if (laneLines[lane].empty()) laneLines[lane].push_back(p1);
                    laneLines[lane].push_back(p2);
                }
            }

            // Fill road
            if (laneLines[0].size() >= 2 && laneLines[laneCount].size() >= 2) {
                // Simple filled polygon approximation
                for (size_t i = 0; i + 1 < laneLines[0].size() && i + 1 < laneLines[laneCount].size(); ++i) {
                    Vector2 p1 = laneLines[0][i];
                    Vector2 p2 = laneLines[0][i + 1];
                    Vector2 p3 = laneLines[laneCount][i + 1];
                    Vector2 p4 = laneLines[laneCount][i];

                    DrawTriangle(p1, p2, p3, Color{180, 180, 180, 255});
                    DrawTriangle(p1, p3, p4, Color{180, 180, 180, 255});
                }

                // Gold edges
                for (size_t i = 0; i + 1 < laneLines[0].size(); ++i) {
                    DrawLineEx(laneLines[0][i], laneLines[0][i + 1], 3.0f, GOLD);
                    DrawLineEx(laneLines[laneCount][i], laneLines[laneCount][i + 1], 3.0f, GOLD);
                }

                // Lane markings
                for (int lane = 1; lane < laneCount; ++lane) {
                    bool isCenter = (lane == GameManager::LanesPerDirection);
                    Color lineCol = isCenter ? YELLOW : WHITE;
                    float thick = isCenter ? 3.0f : 2.0f;

                    for (size_t i = 0; i + 1 < laneLines[lane].size(); ++i) {
                        if (isCenter) {
                            // Double solid
                            Vector2 a = laneLines[lane][i];
                            Vector2 b = laneLines[lane][i + 1];
                            float dx = b.y - a.y;
                            float dy = a.x - b.x;
                            float len = std::sqrt(dx * dx + dy * dy);
                            if (len > 0.1f) {
                                dx /= len; dy /= len;
                                float off = 4.0f;
                                DrawLineEx({a.x + dx * off, a.y + dy * off},
                                           {b.x + dx * off, b.y + dy * off}, thick, lineCol);
                                DrawLineEx({a.x - dx * off, a.y - dy * off},
                                           {b.x - dx * off, b.y - dy * off}, thick, lineCol);
                            }
                        } else {
                            // Dashed
                            if ((i / 2) % 2 == 0) {
                                DrawLineEx(laneLines[lane][i], laneLines[lane][i + 1], thick, lineCol);
                            }
                        }
                    }
                }
            }
        }

        // Draw enemies
        for (const auto& e : game.enemies()) {
            float size = 30.0f;
            float h = size * 1.4f;

            // Shadow
            DrawEllipse(e.worldX, e.worldY + h * 0.45f, size * 0.55f, 6.0f, Color{0, 0, 0, 60});

            // Body
            Rectangle body = { e.worldX - size / 2, e.worldY - h / 2, size, h };
            DrawRectangleRounded(body, 0.25f, 6, ToRayColor(e.color));
            DrawRectangleRoundedLinesEx(body, 0.25f, 6, 1.5f, BLACK);

            // Window
            DrawRectangle(e.worldX - size * 0.28f, e.worldY - h * 0.28f,
                          size * 0.56f, h * 0.22f, Color{200, 230, 255, 220});

            // Lights
            DrawCircle(e.worldX - size * 0.18f, e.worldY - h / 2 - 2, 4, Color{255, 255, 200, 220});
            DrawCircle(e.worldX + size * 0.18f, e.worldY - h / 2 - 2, 4, Color{255, 255, 200, 220});
            DrawCircle(e.worldX - size * 0.18f, e.worldY + h / 2 - 2, 4, Color{180, 30, 30, 200});
            DrawCircle(e.worldX + size * 0.18f, e.worldY + h / 2 - 2, 4, Color{180, 30, 30, 200});
        }

        // Draw player
        {
            const auto& p = game.player();
            float w = p.width();
            float h = p.height();
            float ang = p.angle(); // radians

            // Smoke particles
            for (const auto& s : p.smokeParticles()) {
                Color col;
                if (s.isGrass) {
                    col = Color{100, 200, 100, static_cast<unsigned char>(80 * s.life)};
                } else {
                    col = Color{220, 220, 220, static_cast<unsigned char>(80 * s.life)};
                }
                DrawCircle(s.x, s.y, s.size * 0.5f, col);
            }

            // Shadow
            DrawEllipse(p.worldX(), p.worldY() + h * 0.4f, w * 0.4f, 7.0f, Color{0, 0, 0, 60});

            // We draw the car rotated around its center
            // Raylib doesn't have easy rotated rect, so we use a simple approach
            Vector2 center = { p.worldX(), p.worldY() };

            // Approximate car body with a rotated rectangle using lines / triangles
            float cosA = std::cos(ang);
            float sinA = std::sin(ang);

            auto rotate = [&](float lx, float ly) -> Vector2 {
                return {
                    center.x + lx * cosA - ly * sinA,
                    center.y + lx * sinA + ly * cosA
                };
            };

            // Body points (local space: forward is +X in our original, but we adjust)
            // Original C# used angle where 0 is right. We keep same.
            Vector2 body[4] = {
                rotate(-h / 2, -w / 2),
                rotate( h / 2, -w / 2),
                rotate( h / 2,  w / 2),
                rotate(-h / 2,  w / 2)
            };

            Color bodyCol = ToRayColor(p.color());
            DrawTriangle(body[0], body[1], body[2], bodyCol);
            DrawTriangle(body[0], body[2], body[3], bodyCol);
            DrawLineEx(body[0], body[1], 2.0f, DARKBLUE);
            DrawLineEx(body[1], body[2], 2.0f, DARKBLUE);
            DrawLineEx(body[2], body[3], 2.0f, DARKBLUE);
            DrawLineEx(body[3], body[0], 2.0f, DARKBLUE);

            // Roof
            Vector2 roof[4] = {
                rotate(-h * 0.15f, -w * 0.28f),
                rotate( h * 0.25f, -w * 0.28f),
                rotate( h * 0.25f,  w * 0.28f),
                rotate(-h * 0.15f,  w * 0.28f)
            };
            DrawTriangle(roof[0], roof[1], roof[2], SKYBLUE);
            DrawTriangle(roof[0], roof[2], roof[3], SKYBLUE);

            // Headlights
            Vector2 headL = rotate(h / 2 + 2, -w * 0.25f);
            Vector2 headR = rotate(h / 2 + 2,  w * 0.25f);
            DrawCircle(headL.x, headL.y, 4, Color{255, 255, 180, 220});
            DrawCircle(headR.x, headR.y, 4, Color{255, 255, 180, 220});

            // Drift tint
            if (p.driftIntensity() > 0.2f) {
                unsigned char alpha = static_cast<unsigned char>(std::min(120.f, p.driftIntensity() * 180.f));
                Color driftCol = {255, 40, 40, alpha};
                DrawTriangle(body[0], body[1], body[2], driftCol);
                DrawTriangle(body[0], body[2], body[3], driftCol);
            }
        }

        EndMode2D();

        // --- UI (screen space) ---
        const auto& p = game.player();
        float speed = p.velocity().length();

        DrawText(TextFormat("Speed: %.1f", speed), 10, 10, 22, WHITE);
        DrawText(TextFormat("Score: %d", game.score() / 60), 10, 40, 22, WHITE);
        DrawText(TextFormat("Drift: %.2f", p.driftIntensity()), 10, 70, 18, YELLOW);
        DrawText(TextFormat("Enemies: %d", (int)game.enemies().size()), 10, 95, 18, LIGHTGRAY);

        if (!p.isOnRoad()) {
            DrawText("OFF ROAD!", screenWidth / 2 - 60, 30, 24, RED);
        }

        if (game.isGameOver()) {
            DrawRectangle(0, 0, screenWidth, screenHeight, Color{0, 0, 0, 160});
            const char* go = "GAME OVER";
            int tw = MeasureText(go, 50);
            DrawText(go, screenWidth / 2 - tw / 2, screenHeight / 2 - 40, 50, RED);

            const char* fs = TextFormat("Final Score: %d", game.score() / 60);
            int tw2 = MeasureText(fs, 28);
            DrawText(fs, screenWidth / 2 - tw2 / 2, screenHeight / 2 + 30, 28, YELLOW);

            DrawText("Press R to restart", screenWidth / 2 - 90, screenHeight / 2 + 80, 20, LIGHTGRAY);
        }

        DrawFPS(screenWidth - 90, 10);

        EndDrawing();
    }

    CloseWindow();
    return 0;
}
