#pragma once
#include <vector>
#include <cmath>
#include <random>
#include <optional>

struct RoadSegment {
    float startDistance = 0.f;
    float length = 0.f;
    float startAngle = 0.f;
    float endAngle = 0.f;
};

struct RoadInfo {
    float distance = 0.f;
    float angle = 0.f;
    const RoadSegment* segment = nullptr;
    float segmentProgress = 0.f;
};

class RoadPath {
public:
    RoadPath() {
        generatePath();
    }

    float totalLength() const { return totalLength_; }
    const std::vector<RoadSegment>& segments() const { return segments_; }

    void addSegment(float& currentDistance, float& currentAngle) {
        float segmentLength = 120.f;
        float turnAngle = (randFloat() - 0.5f) * 0.18f;

        RoadSegment segment;
        segment.startDistance = currentDistance;
        segment.length = segmentLength;
        segment.startAngle = currentAngle;
        segment.endAngle = currentAngle + turnAngle;

        segments_.push_back(segment);
        currentDistance += segmentLength;
        currentAngle += turnAngle;
        totalLength_ = currentDistance;
    }

    void removeSegmentsBehind(float minDistance) {
        while (!segments_.empty() &&
               segments_.front().startDistance + segments_.front().length < minDistance) {
            segments_.erase(segments_.begin());
        }
    }

    RoadInfo getRoadInfoAtDistance(float distance) const {
        if (distance < 0.f) distance = 0.f;

        for (const auto& segment : segments_) {
            if (distance >= segment.startDistance &&
                distance < segment.startDistance + segment.length) {
                float t = (distance - segment.startDistance) / segment.length;
                float angle = segment.startAngle + (segment.endAngle - segment.startAngle) * t;
                return {distance, angle, &segment, t};
            }
        }

        if (!segments_.empty()) {
            const auto& last = segments_.back();
            return {distance, last.endAngle, &last, 1.0f};
        }

        return {distance, 0.f, nullptr, 0.f};
    }

    void getWorldPosition(float distance, float& worldX, float& worldY, float& angle) const {
        if (distance < 0.f) distance = 0.f;

        angle = 0.f;
        float currentX = 400.f;
        float currentY = 0.f;

        for (const auto& seg : segments_) {
            float segmentEnd = seg.startDistance + seg.length;

            if (distance >= segmentEnd) {
                float midAngle = (seg.startAngle + seg.endAngle) * 0.5f;
                float dx = seg.length * std::sin(midAngle);
                float dy = -seg.length * std::cos(midAngle);
                currentX += dx;
                currentY += dy;
            } else if (distance >= seg.startDistance && distance < segmentEnd) {
                float t = (distance - seg.startDistance) / seg.length;
                float angleAtT = seg.startAngle + (seg.endAngle - seg.startAngle) * t;
                angle = angleAtT;
                float partialLength = t * seg.length;
                float dx = partialLength * std::sin(angleAtT);
                float dy = -partialLength * std::cos(angleAtT);
                currentX += dx;
                currentY += dy;
                worldX = currentX;
                worldY = currentY;
                return;
            }
        }

        if (!segments_.empty()) {
            angle = segments_.back().endAngle;
        }
        worldX = currentX;
        worldY = currentY;
    }

private:
    std::vector<RoadSegment> segments_;
    float totalLength_ = 0.f;
    mutable std::mt19937 rng_{std::random_device{}()};

    float randFloat() {
        std::uniform_real_distribution<float> dist(0.f, 1.f);
        return dist(rng_);
    }

    void generatePath() {
        segments_.clear();
        totalLength_ = 0.f;
        float currentDistance = 0.f;
        float currentAngle = 0.f;
        for (int i = 0; i < 30; ++i) {
            addSegment(currentDistance, currentAngle);
        }
    }
};
