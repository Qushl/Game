#include "../include/GameManager.hpp"
#include <iostream>
#include <thread>
#include <chrono>
#include <iomanip>

int main() {
    std::cout << "=== Top-Down Highway Drifter — C++ Logic Port ===\n";
    std::cout << "Pure logic module (no rendering yet)\n\n";

    GameManager game(800, 800);

    // Simulate some input: accelerate + slight left turns
    bool accelerate = true;
    bool left = false;
    bool right = false;

    const int frames = 300; // ~5 seconds at 60 FPS

    for (int frame = 0; frame < frames; ++frame) {
        // Simple AI-like input for demo
        if (frame > 60 && frame < 120) left = true;
        else left = false;

        if (frame > 180 && frame < 240) right = true;
        else right = false;

        game.player().setInput(left, right, accelerate, false);
        game.update();
        game.updateCamera();

        if (frame % 30 == 0) {
            const auto& p = game.player();
            std::cout << std::fixed << std::setprecision(1)
                      << "Frame " << std::setw(3) << frame
                      << " | Pos: (" << std::setw(7) << p.worldX()
                      << ", " << std::setw(7) << p.worldY() << ")"
                      << " | Speed: " << std::setw(5) << p.velocity().length()
                      << " | Drift: " << std::setw(4) << p.driftIntensity()
                      << " | OnRoad: " << (p.isOnRoad() ? "YES" : "NO ")
                      << " | Enemies: " << std::setw(2) << game.enemies().size()
                      << " | Score: " << (game.score() / 60)
                      << (game.isGameOver() ? "  *** GAME OVER ***" : "")
                      << "\n";
        }

        if (game.isGameOver()) {
            std::cout << "\nGame Over at frame " << frame << "!\n";
            break;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(16));
    }

    std::cout << "\n=== Simulation finished ===\n";
    std::cout << "Final score: " << (game.score() / 60) << "\n";
    std::cout << "Enemies left: " << game.enemies().size() << "\n";
    std::cout << "Player position: (" << game.player().worldX()
              << ", " << game.player().worldY() << ")\n";

    return 0;
}
