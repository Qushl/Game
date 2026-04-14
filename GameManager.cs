using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TopDownHighwayDrifter
{
    public class GameManager
    {
        public PlayerCar Player { get; private set; }
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        
        private RoadPath _roadPath;
        private int _spawnCounter = 0;
        private int _spawnRate = 50;
        private Random _random = new Random();
        
        private int _score = 0;
        private bool _gameOver = false;
        
        private const float RoadWidth = 150f;
        private const float ScreenWidth = 800f;
        private const float ScreenHeight = 800f;

        public int Score => _score;
        public bool IsGameOver => _gameOver;
        public RoadPath RoadPath => _roadPath;

        public GameManager(int screenWidth, int screenHeight)
        {
            _roadPath = new RoadPath();
            Player = new PlayerCar();
            Player.WorldX = 400;
            Player.WorldY = -500;
        }

        public void Update()
        {
            if (_gameOver)
                return;

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

            // Обновляем врагов и удаляем тех, которые далеко позади
            var enemiesToRemove = new List<Enemy>();
            foreach (var enemy in Enemies)
            {
                enemy.WorldY += 3f;
                
                float distToPlayer = (float)Math.Sqrt(
                    Math.Pow(enemy.WorldX - Player.WorldX, 2) + 
                    Math.Pow(enemy.WorldY - Player.WorldY, 2)
                );
                
                // Добавляем очки за проезд рядом с врагом
                const float passDistance = 80f; 
                if (distToPlayer < passDistance && !enemy.WasRewarded)
                {
                    _score += 600; // +10 очков (поскольку очки увеличиваются каждый кадр)
                    enemy.WasRewarded = true;
                }
                
                if (distToPlayer > 3000)
                {
                    enemiesToRemove.Add(enemy);
                }
            }

            foreach (var enemy in enemiesToRemove)
            {
                Enemies.Remove(enemy);
            }

            // Увеличиваем очки за каждый кадр выживания
            _score++;

            // Увеличиваем сложность со временем
            _spawnRate = Math.Max(15, 50 - _score / 360);
        }

        private void SpawnEnemy()
        {
            float spawnX = Player.WorldX + (_random.NextSingle() - 0.5f) * 200;
            float spawnY = Player.WorldY - 800; // Спавним врагов выше

            Color[] enemyColors = new Color[]
            {
                Color.Red,
                Color.Green,
                Color.Purple,
                Color.Orange,
                Color.Cyan,
                Color.Yellow
            };

            var enemy = new Enemy
            {
                WorldX = spawnX,
                WorldY = spawnY,
                Color = enemyColors[_random.Next(enemyColors.Length)]
            };
            
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

            // Находим текущее расстояние на дороге на основе WorldY
            float playerDistanceOnRoad = -Player.WorldY; 
            
            float startDist = Math.Max(0, playerDistanceOnRoad - visibleDistance);
            float endDist = playerDistanceOnRoad + visibleDistance;

            var leftEdgePoints = new List<PointF>();
            var rightEdgePoints = new List<PointF>();

            // Собираем видимые сегменты дороги
            for (int i = 0; i < _roadPath.Segments.Count; i++)
            {
                var segment = _roadPath.Segments[i];
                float segDist1 = segment.StartDistance;
                float segDist2 = segment.StartDistance + segment.Length;

                if (segDist2 < startDist || segDist1 > endDist)
                    continue;

                _roadPath.GetWorldPosition(segDist1, out float x1, out float y1, out float angle1);
                _roadPath.GetWorldPosition(segDist2, out float x2, out float y2, out float angle2);

                y1 = -y1;
                y2 = -y2;

                float roadWidthHalf = RoadWidth / 2;

                // Направление вдоль дороги
                float dx = x2 - x1;
                float dy = y2 - y1;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (len < 0.1f)
                    continue;

                dx /= len;
                dy /= len;

                float perpX = -dy;
                float perpY = dx;

                // Края сегмента
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

                    using (var roadBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                    {
                        g.FillPath(roadBrush, roadPath);
                    }

                    using (var edgePen = new Pen(Color.Gold, 3) { LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel })
                    {
                        g.DrawPath(edgePen, roadPath);
                    }
                }

                DrawRoadMarkings(g, leftEdgePoints, rightEdgePoints);
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
        public Color Color { get; set; }
        public bool WasRewarded { get; set; } = false; 
    }
}

