using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TopDownHighwayDrifter
{
    public class GameManager
    {
        public PlayerCar Player { get; private set; }
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        
        private RoadPath _roadPath;
        private float _cameraAngle = 0f;
        private const float CameraTurnLerp = 0.04f;
        private int _spawnCounter = 0;
        private int _spawnRate = 50;
        private Random _random = new Random();
        
        private int _score = 0;
        private bool _gameOver = false;
        
        private const float RoadWidth = 150f;
        private const float ScreenWidth = 800f;
        private const float ScreenHeight = 800f;

        // Многопоточность
        private object _lockObject = new object();
        private bool _updateInProgress = false;

        public int Score => _score;
        public bool IsGameOver => _gameOver;
        public RoadPath RoadPath => _roadPath;

        public GameManager(int screenWidth, int screenHeight)
        {
            _roadPath = new RoadPath();
            Player = new PlayerCar();
            // Старт по центру экрана, чтобы дорога была видна
            Player.WorldX = screenWidth / 2;
            Player.WorldY = 0;
            Player.Angle = (float)(-Math.PI / 2); // Вверх по экрану
        }

        public void Update()
        {
            if (_gameOver || _updateInProgress)
                return;

            // Запускаем обновление в отдельном потоке
            Task.Run(() => UpdateGameLogic());
        }

        private void UpdateGameLogic()
        {
            _updateInProgress = true;
            try
            {
                lock (_lockObject)
                {
                    if (_gameOver)
                        return;

                    // Проверяем, находится ли игрок на дороге
                    Player.IsOnRoad = IsPlayerOnRoad();

                    // Обновляем физику игрока
                    Player.Update(0);

                    // Проверяем столкновения
                    CheckCollisions();

                    // Создание новых врагов
                    _spawnCounter++;
                    if (_spawnCounter > _spawnRate)
                    {
                        SpawnEnemy();
                        _spawnCounter = 0;
                        _spawnRate = Math.Max(20, _spawnRate - 1);
                    }

                    // Обновляем врагов по траектории дороги
                    float playerDistanceOnRoad = -Player.WorldY;
                    
                    // Обновляем врагов параллельно
                    Parallel.ForEach(Enemies, enemy =>
                    {
                        if (enemy.IsOncoming)
                        {
                            enemy.Distance -= enemy.Speed;
                        }
                        else
                        {
                            enemy.Distance += enemy.Speed;
                        }
                        UpdateEnemyWorldPosition(enemy);
                    });

                    // Удаляем врагов, которые далеко позади
                    Enemies.RemoveAll(enemy => 
                    {
                        float distToPlayer = (float)Math.Sqrt(
                            Math.Pow(enemy.WorldX - Player.WorldX, 2) + 
                            Math.Pow(enemy.WorldY - Player.WorldY, 2)
                        );
                        
                        // Добавляем очки за проезд рядом с врагом
                        const float passDistance = 80f; 
                        if (distToPlayer < passDistance && !enemy.WasRewarded)
                        {
                            _score += 600;
                            enemy.WasRewarded = true;
                        }
                        
                        return enemy.Distance < playerDistanceOnRoad - 3000;
                    });


                    // Увеличиваем или уменьшаем очки за каждый кадр
                    if (Player.IsOnRoad)
                    {
                        _score++;
                    }
                    else
                    {
                        // На обочине — медленно отнимаем очки, но не уходим в минус
                        if (_score > 0)
                            _score -= 2; // Можно скорректировать скорость штрафа
                    }

                    // Увеличиваем сложность со временем (медленнее)
                    _spawnRate = Math.Max(35, 60 - _score / 1000);
                }
            }
            finally
            {
                _updateInProgress = false;
            }
        }

        private void SpawnEnemy()
        {
            // Спавним врага впереди на дороге
            float playerDistanceOnRoad = -Player.WorldY;
            float spawnDist = playerDistanceOnRoad + 800;
            _roadPath.GetWorldPosition(spawnDist, out float spawnX, out float spawnY, out float angle);

            Color[] enemyColors = new Color[]
            {
                Color.Red,
                Color.Green,
                Color.Purple,
                Color.Orange,
                Color.Cyan,
                Color.Yellow
            };

            float minLane = RoadWidth * 0.25f;
            float maxLane = RoadWidth * 0.45f;
            float laneOffset = (float)(minLane + _random.NextDouble() * (maxLane - minLane));
            bool isLeftLane = _random.Next(2) == 0;
            laneOffset *= isLeftLane ? -1f : 1f;
            float speed = 0.6f + (float)_random.NextDouble() * 0.6f;
            var enemy = new Enemy
            {
                Distance = spawnDist,
                Speed = speed,
                LaneOffset = laneOffset,
                IsOncoming = isLeftLane,
                Color = enemyColors[_random.Next(enemyColors.Length)]
            };

            UpdateEnemyWorldPosition(enemy);
            
            Enemies.Add(enemy);
        }

        private void CheckCollisions()
        {
            const float collisionRadius = 30f;

            foreach (var enemy in Enemies)
            {
                float dx = Player.WorldX - enemy.WorldX;
                float dy = Player.WorldY - enemy.WorldY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance < collisionRadius)
                {
                    _gameOver = true;
                }
            }
        }

        public void DrawGame(Graphics g, int screenWidth, int screenHeight)
        {
            g.Clear(Color.FromArgb(34, 139, 34)); // Зелёный фон (трава)

            // Применяем трансформацию для следования за игроком
            var state = g.Save();

            g.TranslateTransform(screenWidth / 2, screenHeight / 2);
            float targetAngle = GetCameraTargetAngle();
            _cameraAngle = LerpAngle(_cameraAngle, targetAngle, CameraTurnLerp);
            float cameraAngleDeg = (float)(_cameraAngle * 180.0 / Math.PI);
            g.RotateTransform(cameraAngleDeg);
            
            g.TranslateTransform(-Player.WorldX, -Player.WorldY);

            var sortedEnemies = new List<Enemy>(Enemies);
            sortedEnemies.Sort((a, b) => a.WorldY.CompareTo(b.WorldY));

            DrawRoadOnScreen(g);

            foreach (var enemy in sortedEnemies)
            {
                DrawEnemy(g, enemy);
            }

            Player.Draw(g);

            g.Restore(state);

            DrawUI(g, screenWidth, screenHeight);
        }

        private void DrawRoadOnScreen(Graphics g)
        {
            const float visibleDistance = 2500f;
            float playerDistanceOnRoad = -Player.WorldY;
            float startDist = playerDistanceOnRoad - visibleDistance; // Может быть отрицательным
            float endDist = playerDistanceOnRoad + visibleDistance;

            // Генерируем сегменты, чтобы гарантировать достаточное покрытие
            float currentDist = _roadPath.TotalLength;
            float currentAngle = _roadPath.Segments.Count > 0 ? _roadPath.Segments[^1].EndAngle : 0;
            // Генерируем ДО конца видимой области + запас
            while (_roadPath.TotalLength < endDist + 2000)
            {
                _roadPath.AddSegment(ref currentDist, ref currentAngle);
            }

            var leftEdgePoints = new List<PointF>();
            var rightEdgePoints = new List<PointF>();

            float step = 30f;
            for (float dist = startDist; dist < endDist; dist += step)
            {
                float segDist1 = dist;
                float segDist2 = dist + step;
                _roadPath.GetWorldPosition(segDist1, out float x1, out float y1, out float angle1);
                _roadPath.GetWorldPosition(segDist2, out float x2, out float y2, out float angle2);

                float roadWidthHalf = RoadWidth / 2;
                float dx = x2 - x1;
                float dy = y2 - y1;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.1f)
                    continue;
                dx /= len;
                dy /= len;
                float perpX = -dy;
                float perpY = dx;

                PointF leftStart = new PointF(x1 - perpX * roadWidthHalf, y1 - perpY * roadWidthHalf);
                PointF rightStart = new PointF(x1 + perpX * roadWidthHalf, y1 + perpY * roadWidthHalf);
                PointF leftEnd = new PointF(x2 - perpX * roadWidthHalf, y2 - perpY * roadWidthHalf);
                PointF rightEnd = new PointF(x2 + perpX * roadWidthHalf, y2 + perpY * roadWidthHalf);

                if (leftEdgePoints.Count == 0)
                    leftEdgePoints.Add(leftStart);
                leftEdgePoints.Add(leftEnd);

                if (rightEdgePoints.Count == 0)
                    rightEdgePoints.Add(rightStart);
                rightEdgePoints.Add(rightEnd);
            }

            // Рисуем дорогу
            if (leftEdgePoints.Count >= 2 && rightEdgePoints.Count >= 2)
            {
                using (var roadPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    roadPath.AddLines(leftEdgePoints.ToArray());

                    for (int i = rightEdgePoints.Count - 1; i >= 0; i--)
                    {
                        roadPath.AddLine(roadPath.GetLastPoint(), rightEdgePoints[i]);
                    }

                    roadPath.CloseFigure();

                    using (var roadBrush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                    {
                        g.FillPath(roadBrush, roadPath);
                    }

                    using (var edgePen = new Pen(Color.Gold, 3) { LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel })
                    {
                        g.DrawPath(edgePen, roadPath);
                    }
                }

                DrawRoadMarkings(g, leftEdgePoints, rightEdgePoints);
                DrawCenterLine(g, leftEdgePoints, rightEdgePoints);
            }
        }

        // Жёлтая пунктирная центральная линия
        private void DrawCenterLine(Graphics g, List<PointF> leftEdge, List<PointF> rightEdge)
        {
            if (leftEdge.Count < 2 || rightEdge.Count < 2)
                return;
            using (var pen = new Pen(Color.Yellow, 3) { DashPattern = new float[] { 16, 16 } })
            {
                for (int i = 0; i < leftEdge.Count && i < rightEdge.Count; i++)
                {
                    var mid = new PointF(
                        (leftEdge[i].X + rightEdge[i].X) / 2,
                        (leftEdge[i].Y + rightEdge[i].Y) / 2
                    );
                    if (i > 0)
                    {
                        var prevMid = new PointF(
                            (leftEdge[i - 1].X + rightEdge[i - 1].X) / 2,
                            (leftEdge[i - 1].Y + rightEdge[i - 1].Y) / 2
                        );
                        g.DrawLine(pen, prevMid, mid);
                    }
                }
            }
        }

        private void DrawRoadMarkings(Graphics g, List<PointF> leftEdge, List<PointF> rightEdge)
        {
            if (leftEdge.Count < 2)
                return;

            using (var markingPen = new Pen(Color.White, 1.5f))
            {
                for (int i = 0; i < leftEdge.Count - 1; i++)
                {
                    if (i % 3 == 0)
                    {
                        float midX1 = (leftEdge[i].X + rightEdge[i].X) / 2;
                        float midY1 = (leftEdge[i].Y + rightEdge[i].Y) / 2;

                        float midX2 = (leftEdge[i + 1].X + rightEdge[i + 1].X) / 2;
                        float midY2 = (leftEdge[i + 1].Y + rightEdge[i + 1].Y) / 2;

                        g.DrawLine(markingPen, midX1, midY1, midX2, midY2);
                    }
                }
            }
        }

        private void DrawEnemy(Graphics g, Enemy enemy)
        {
            float size = 20;
            
            // Тень
            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, enemy.WorldX - size / 2 - 2, enemy.WorldY + size / 2, size + 4, 4);
            }

            // Кузов
            using (var brush = new SolidBrush(enemy.Color))
            {
                g.FillRectangle(brush, enemy.WorldX - size / 2, enemy.WorldY - size / 2, size, size);
            }

            // Окно
            using (var windowBrush = new SolidBrush(Color.LightBlue))
            {
                g.FillRectangle(windowBrush, enemy.WorldX - size / 2 + 2, enemy.WorldY - size / 2 + 2, size - 4, 6);
            }

            // Контур
            using (var pen = new Pen(Color.Black, 1.5f))
            {
                g.DrawRectangle(pen, enemy.WorldX - size / 2, enemy.WorldY - size / 2, size, size);
            }
        }

        private void DrawUI(Graphics g, int screenWidth, int screenHeight)
        {
            using (var font = new Font("Arial", 16, FontStyle.Bold))
            {
                float speed = Player.Velocity.Length();
                string speedText = $"Speed: {speed:F1}";
                var brush = new SolidBrush(Color.White);
                g.DrawString(speedText, font, brush, 10, 10);
                brush.Dispose();
            }

            using (var font = new Font("Arial", 16, FontStyle.Bold))
            {
                string scoreText = $"Score: {_score / 60}";
                var brush = new SolidBrush(Color.White);
                g.DrawString(scoreText, font, brush, 10, 40);
                brush.Dispose();
            }

            using (var font = new Font("Arial", 12))
            {
                string driftText = $"Drift: {Player.DriftIntensity:F2}";
                var brush = new SolidBrush(Color.Yellow);
                g.DrawString(driftText, font, brush, 10, 70);
                brush.Dispose();
            }

            if (_gameOver)
            {
                using (var font = new Font("Arial", 40, FontStyle.Bold))
                {
                    string gameOverText = "GAME OVER";
                    SizeF size = g.MeasureString(gameOverText, font);
                    float x = (screenWidth - size.Width) / 2;
                    float y = (screenHeight - size.Height) / 2;

                    using (var blackBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                    {
                        g.FillRectangle(blackBrush, x - 20, y - 20, size.Width + 40, size.Height + 40);
                    }

                    using (var brush = new SolidBrush(Color.Red))
                    {
                        g.DrawString(gameOverText, font, brush, x, y);
                    }
                }

                using (var font = new Font("Arial", 20))
                {
                    string finalScoreText = $"Final Score: {_score / 60}";
                    SizeF size = g.MeasureString(finalScoreText, font);
                    float x = (screenWidth - size.Width) / 2;
                    float y = (screenHeight - size.Height) / 2 + 60;

                    using (var brush = new SolidBrush(Color.Yellow))
                    {
                        g.DrawString(finalScoreText, font, brush, x, y);
                    }
                }
            }
        }

        private void UpdateEnemyWorldPosition(Enemy enemy)
        {
            _roadPath.GetWorldPosition(enemy.Distance, out float x, out float y, out float angle);

            float perpX = (float)Math.Cos(angle);
            float perpY = (float)Math.Sin(angle);

            enemy.WorldX = x + perpX * enemy.LaneOffset;
            enemy.WorldY = y + perpY * enemy.LaneOffset;
        }

        private float GetCameraTargetAngle()
        {
            float speed = Player.Velocity.Length();
            if (speed > 0.3f)
            {
                float heading = (float)Math.Atan2(Player.Velocity.Y, Player.Velocity.X);
                return (float)(-heading - Math.PI / 2);
            }

            return (float)(-Player.Angle - Math.PI / 2);
        }

        private static float LerpAngle(float current, float target, float t)
        {
            float diff = target - current;
            while (diff > Math.PI) diff -= (float)(2 * Math.PI);
            while (diff < -Math.PI) diff += (float)(2 * Math.PI);
            return current + diff * t;
        }

        private bool IsPlayerOnRoad()
        {
            // Получаем информацию о дороге на позиции игрока
            float playerDistanceOnRoad = -Player.WorldY;
            _roadPath.GetWorldPosition(playerDistanceOnRoad, out float roadCenterX, out float roadCenterY, out float roadAngle);

            // Вычисляем перпендикулярное расстояние от центра дороги
            float dx = Player.WorldX - roadCenterX;
            float dy = Player.WorldY - roadCenterY;

            // Проецируем на перпендикуляр к дороге
            float perpX = (float)Math.Cos(roadAngle);
            float perpY = (float)Math.Sin(roadAngle);
            
            float lateralDistance = Math.Abs(dx * perpX + dy * perpY);
            
            const float roadWidthHalf = RoadWidth / 2;
            return lateralDistance < roadWidthHalf;
        }

        public void Reset()
        {
            Player = new PlayerCar();
            Player.WorldX = 400;
            Player.WorldY = -500;
            Enemies.Clear();
            _score = 0;
            _gameOver = false;
            _spawnRate = 50;
            _spawnCounter = 0;
        }
    }

    public class Enemy
    {
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float Distance { get; set; }
        public float Speed { get; set; }
        public float LaneOffset { get; set; }
        public bool IsOncoming { get; set; }
        public Color Color { get; set; }
        public bool WasRewarded { get; set; } = false; 
    }
}

