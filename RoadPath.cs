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

            // Создаём дорогу с поворотами
            // Каждый сегмент имеет длину и кривизну (угол поворота)
            Random rand = new Random(42); // Фиксированный seed для воспроизводимости

            float currentDistance = 0;
            float currentAngle = 0;

            // Генерируем 50 сегментов (каждый ~100 пикселей)
            for (int i = 0; i < 100; i++)
            {
                float segmentLength = 100f; // Длина прямого сегмента
                float turnAngle = (float)(rand.NextDouble() - 0.5) * 0.3f; // Случайный поворот ±0.15 радиан

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
        }

        /// <summary>
        /// Получить информацию о дороге на заданном расстоянии
        /// </summary>
        public RoadInfo GetRoadInfoAtDistance(float distance)
        {
            // Бесконечная дорога - циклируем
            distance = distance % _totalLength;
            if (distance < 0) distance += _totalLength;

            foreach (var segment in _segments)
            {
                if (distance >= segment.StartDistance && 
                    distance < segment.StartDistance + segment.Length)
                {
                    // прогресс внутри сегмента
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

            // Fallback
            return new RoadInfo
            {
                Distance = distance,
                Angle = 0,
                Segment = _segments[0],
                SegmentProgress = 0
            };
        }

        /// <summary>
        /// Получить мировую позицию и угол для точки на дороге
        /// </summary>
        public void GetWorldPosition
        (
            float distance, 
            out float worldX, 
            out float worldY, 
            out float angle)
        {
            RoadInfo info = GetRoadInfoAtDistance(distance);
            angle = info.Angle;

            float currentX = 400;
            float currentY = 0; 

            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                float segmentEnd = seg.StartDistance + seg.Length;

                if (distance >= segmentEnd)
                {
                    // Прошли этот сегмент полностью
                    // Используем средний угол сегмента для расчета движения
                    float midAngle = (seg.StartAngle + seg.EndAngle) / 2;
                    
                    // Движение ВВЕРХ по экрану = отрицательное Y
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
                    
                    float partialLength = t * seg.Length;
                    float dx = partialLength * (float)Math.Sin(angleAtT);
                    float dy = -partialLength * (float)Math.Cos(angleAtT);
                    
                    currentX += dx;
                    currentY += dy;
                    break;
                }
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
