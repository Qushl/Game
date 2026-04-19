using System;
using System.Collections.Generic;

namespace TopDownHighwayDrifter
{
    /// <summary>
    /// Представляет путь дороги с поворотами и кривыми
    /// </summary>
    public class RoadPath
    {
        private List<RoadSegment> _segments = new List<RoadSegment>();
        private float _totalLength = 0;
        private Random _rand = new Random();

        public float TotalLength => _totalLength;
        public List<RoadSegment> Segments => _segments;

        public RoadPath()
        {
            GeneratePath();
        }

        private void GeneratePath()
        {
            _segments.Clear();
            _totalLength = 0;
            float currentDistance = 0;
            float currentAngle = 0;
            for (int i = 0; i < 30; i++)
            {
                AddSegment(ref currentDistance, ref currentAngle);
            }
        }

        // Добавить сегмент в конец
        public void AddSegment(ref float currentDistance, ref float currentAngle)
        {
            float segmentLength = 120f;
            float turnAngle = (float)(_rand.NextDouble() - 0.5) * 0.18f;
            var segment = new RoadSegment
            {
                StartDistance = currentDistance,
                Length = segmentLength,
                StartAngle = currentAngle,
                EndAngle = currentAngle + turnAngle
            };
            _segments.Add(segment);
            currentDistance += segmentLength;
            currentAngle += turnAngle;
            _totalLength = currentDistance;
        }

        // Удалить сегменты, которые далеко позади
        public void RemoveSegmentsBehind(float minDistance)
        {
            while (_segments.Count > 0 && _segments[0].StartDistance + _segments[0].Length < minDistance)
            {
                _segments.RemoveAt(0);
            }
        }

        /// <summary>
        /// Получить информацию о дороге на заданном расстоянии
        /// </summary>
        public RoadInfo GetRoadInfoAtDistance(float distance)
        {
            // Поиск сегмента без циклирования
            if (distance < 0) distance = 0;

            foreach (var segment in _segments)
            {
                if (distance >= segment.StartDistance && 
                    distance < segment.StartDistance + segment.Length)
                {
                    float t = (distance - segment.StartDistance) / segment.Length; 
                    float angle = segment.StartAngle + (segment.EndAngle - segment.StartAngle) * t;

                    return new RoadInfo
                    {
                        Distance = distance,
                        Angle = angle,
                        Segment = segment,
                        SegmentProgress = t
                    };
                }
            }

            // Если далеко впереди - верни последний сегмент
            if (_segments.Count > 0)
            {
                var lastSeg = _segments[^1];
                return new RoadInfo
                {
                    Distance = distance,
                    Angle = lastSeg.EndAngle,
                    Segment = lastSeg,
                    SegmentProgress = 1.0f
                };
            }
            return new RoadInfo
            {
                Distance = distance,
                Angle = 0,
                Segment = null,
                SegmentProgress = 0
            };
        }

        /// <summary>
        /// Получить мировую позицию и угол для точки на дороге
        /// </summary>
        public void GetWorldPosition(
            float distance, 
            out float worldX, 
            out float worldY, 
            out float angle)
        {
            if (distance < 0) distance = 0;
            
            angle = 0;
            float currentX = 400;
            float currentY = 0;

            // Проходим через все сегменты до интересующего расстояния
            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                float segmentEnd = seg.StartDistance + seg.Length;

                if (distance >= segmentEnd)
                {
                    // Целый сегмент пройден
                    float midAngle = (seg.StartAngle + seg.EndAngle) / 2;
                    float dx = seg.Length * (float)Math.Sin(midAngle);
                    float dy = -seg.Length * (float)Math.Cos(midAngle);
                    currentX += dx;
                    currentY += dy;
                }
                else if (distance >= seg.StartDistance && distance < segmentEnd)
                {
                    // Находимся на этом сегменте
                    float t = (distance - seg.StartDistance) / seg.Length;
                    float angleAtT = seg.StartAngle + (seg.EndAngle - seg.StartAngle) * t;
                    angle = angleAtT;
                    
                    float partialLength = t * seg.Length;
                    float dx = partialLength * (float)Math.Sin(angleAtT);
                    float dy = -partialLength * (float)Math.Cos(angleAtT);
                    currentX += dx;
                    currentY += dy;
                    worldX = currentX;
                    worldY = currentY;
                    return;
                }
            }

            // Если вышли за пределы сегментов
            if (_segments.Count > 0)
            {
                angle = _segments[^1].EndAngle;
            }
            worldX = currentX;
            worldY = currentY;
        }
    }

    public class RoadSegment
    {
        public float StartDistance { get; set; }
        public float Length { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
    }

    public class RoadInfo
    {
        public float Distance { get; set; }
        public float Angle { get; set; }
        public RoadSegment Segment { get; set; }
        public float SegmentProgress { get; set; }
    }
}
