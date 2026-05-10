using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        private const int LanesPerDirection = 4;
        private const int LaneCount = LanesPerDirection * 2;
        private const float LaneWidth = 72f;
        private const float RoadWidth = LaneCount * LaneWidth;
        private const float ScreenWidth = 800f;
        private const float ScreenHeight = 800f;

        private object _lockObject = new object();
        private bool _updateInProgress = false;

        public int MaxEnemies { get; set; } = 12;
        private PlayerCar.CarModelType _currentModel = PlayerCar.CarModelType.Default;

        public int Score => _score;
        public bool IsGameOver => _gameOver;
        public RoadPath RoadPath => _roadPath;

        public GameManager(int screenWidth, int screenHeight)
        {
            _roadPath = new RoadPath();
            Player = new PlayerCar();
            SetPlayerModel(_currentModel);
            Player.WorldX = screenWidth / 2;
            Player.WorldY = 0;
            Player.Angle = (float)(-Math.PI / 2);
        }

        public void Update()
        {
            if (_gameOver || _updateInProgress) return;
            Task.Run(() => UpdateGameLogic());
        }

        private void UpdateGameLogic()
        {
            _updateInProgress = true;
            try
            {
                lock (_lockObject)
                {
                    if (_gameOver) return;

                    Player.IsOnRoad = IsPlayerOnRoad();
                    Player.Update(0);
                    CheckCollisions();

                    _spawnCounter++;
                    if (_spawnCounter > _spawnRate)
                    {
                        SpawnEnemy();
                        _spawnCounter = 0;
                        _spawnRate = Math.Max(20, _spawnRate - 1);
                    }

                    float playerDistanceOnRoad = -Player.WorldY;

                    Parallel.ForEach(Enemies, enemy =>
                    {
                        if (enemy.IsOncoming) enemy.Distance -= enemy.Speed;
                        else enemy.Distance += enemy.Speed;
                        UpdateEnemyWorldPosition(enemy);
                    });

                    Enemies.RemoveAll(enemy =>
                    {
                        float distToPlayer = (float)Math.Sqrt(Math.Pow(enemy.WorldX - Player.WorldX, 2) + Math.Pow(enemy.WorldY - Player.WorldY, 2));
                        const float passDistance = 80f;
                        if (distToPlayer < passDistance && !enemy.WasRewarded)
                        {
                            _score += 600;
                            enemy.WasRewarded = true;
                        }

                        return enemy.Distance < playerDistanceOnRoad - 3000;
                    });

                    if (Player.IsOnRoad) _score++;
                    else if (_score > 0) _score -= 2;

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
            if (Enemies.Count >= MaxEnemies) return;
            float playerDistanceOnRoad = -Player.WorldY;
            float spawnDistAhead = playerDistanceOnRoad + 900;
            _roadPath.GetWorldPosition(spawnDistAhead, out float spawnXf, out float spawnYf, out float angleF);

            Color[] enemyColors = new Color[] { Color.Red, Color.Green, Color.Purple, Color.Orange, Color.Cyan, Color.Yellow };

            int enemiesForward = _random.Next(LanesPerDirection - 1, LanesPerDirection + 1);
            List<int> usedLanesF = new List<int>();
            for (int i = 0; i < enemiesForward; i++)
            {
                if (Enemies.Count >= MaxEnemies) break;
                int lane;
                do { lane = _random.Next(LanesPerDirection, LaneCount); } while (usedLanesF.Contains(lane));
                usedLanesF.Add(lane);
                float laneOffset = -RoadWidth / 2 + LaneWidth / 2 + lane * LaneWidth;
                bool occupied = Enemies.Any(e => !e.IsOncoming && Math.Abs(e.LaneOffset - laneOffset) < 1f && Math.Abs(e.Distance - spawnDistAhead) < LaneWidth);
                if (occupied) continue;
                float speed = 0.8f + (float)_random.NextDouble() * 0.8f;
                var enemy = new Enemy { Distance = spawnDistAhead, Speed = speed, LaneOffset = laneOffset, IsOncoming = false, Color = enemyColors[_random.Next(enemyColors.Length)] };
                UpdateEnemyWorldPosition(enemy);
                Enemies.Add(enemy);
            }

            int enemiesBackward = _random.Next(LanesPerDirection - 1, LanesPerDirection + 1);
            List<int> usedLanesB = new List<int>();
            for (int i = 0; i < enemiesBackward; i++)
            {
                if (Enemies.Count >= MaxEnemies) break;
                int lane;
                do { lane = _random.Next(0, LanesPerDirection); } while (usedLanesB.Contains(lane));
                usedLanesB.Add(lane);
                float laneOffset = -RoadWidth / 2 + LaneWidth / 2 + lane * LaneWidth;
                bool occupied = Enemies.Any(e => e.IsOncoming && Math.Abs(e.LaneOffset - laneOffset) < 1f && Math.Abs(e.Distance - spawnDistAhead) < LaneWidth);
                if (occupied) continue;
                float speed = 0.8f + (float)_random.NextDouble() * 0.8f;
                var enemy = new Enemy { Distance = spawnDistAhead, Speed = speed, LaneOffset = laneOffset, IsOncoming = true, Color = enemyColors[_random.Next(enemyColors.Length)] };
                UpdateEnemyWorldPosition(enemy);
                Enemies.Add(enemy);
            }
        }

        public void SetPlayerModel(PlayerCar.CarModelType model)
        {
            _currentModel = model;
            if (Player != null) Player.SetModel(model);
        }

        private void CheckCollisions()
        {
            float collisionRadius = Math.Max(30f, Math.Max(Player.Width, Player.Height) * 0.6f);
            foreach (var enemy in Enemies)
            {
                float dx = Player.WorldX - enemy.WorldX;
                float dy = Player.WorldY - enemy.WorldY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance < collisionRadius) _gameOver = true;
            }
        }

        public void DrawGame(Graphics g, int screenWidth, int screenHeight)
        {
            g.Clear(Color.FromArgb(34, 139, 34));
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
            foreach (var enemy in sortedEnemies) DrawEnemy(g, enemy);
            Player.Draw(g);

            g.Restore(state);
            DrawUI(g, screenWidth, screenHeight);
        }

        private void DrawRoadOnScreen(Graphics g)
        {
            const float visibleDistance = 2500f;
            float playerDistanceOnRoad = -Player.WorldY;
            float startDist = playerDistanceOnRoad - visibleDistance;
            float endDist = playerDistanceOnRoad + visibleDistance;

            float currentDist = _roadPath.TotalLength;
            float currentAngle = _roadPath.Segments.Count > 0 ? _roadPath.Segments[^1].EndAngle : 0;
            while (_roadPath.TotalLength < endDist + 2000) _roadPath.AddSegment(ref currentDist, ref currentAngle);

            List<List<PointF>> laneLines = new List<List<PointF>>();
            List<List<float>> laneLineDists = new List<List<float>>();
            for (int lane = 0; lane <= LaneCount; lane++)
            {
                laneLines.Add(new List<PointF>());
                laneLineDists.Add(new List<float>());
            }

            float step = 30f;
            for (float dist = startDist; dist < endDist; dist += step)
            {
                float segDist1 = dist;
                float segDist2 = dist + step;
                _roadPath.GetWorldPosition(segDist1, out float x1, out float y1, out float angle1);
                _roadPath.GetWorldPosition(segDist2, out float x2, out float y2, out float angle2);

                float dx = x2 - x1;
                float dy = y2 - y1;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.1f) continue;
                dx /= len; dy /= len;
                float perpX = -dy;
                float perpY = dx;

                for (int lane = 0; lane <= LaneCount; lane++)
                {
                    float offset = (lane - LaneCount / 2.0f) * LaneWidth;
                    PointF p1 = new PointF(x1 + perpX * offset, y1 + perpY * offset);
                    PointF p2 = new PointF(x2 + perpX * offset, y2 + perpY * offset);
                    if (laneLines[lane].Count == 0)
                    {
                        laneLines[lane].Add(p1);
                        laneLineDists[lane].Add(segDist1);
                    }
                    laneLines[lane].Add(p2);
                    laneLineDists[lane].Add(segDist2);
                }
            }

            if (laneLines[0].Count >= 2 && laneLines[LaneCount].Count >= 2)
            {
                using (var roadPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    roadPath.AddLines(laneLines[0].ToArray());
                    for (int i = laneLines[LaneCount].Count - 1; i >= 0; i--) roadPath.AddLine(roadPath.GetLastPoint(), laneLines[LaneCount][i]);
                    roadPath.CloseFigure();
                    using (var roadBrush = new SolidBrush(Color.FromArgb(180, 180, 180))) g.FillPath(roadBrush, roadPath);
                    using (var edgePen = new Pen(Color.Gold, 3) { LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel }) g.DrawPath(edgePen, roadPath);
                }

                for (int lane = 1; lane < LaneCount; lane++)
                {
                    if (lane == LanesPerDirection) DrawDoubleSolidLine(g, laneLines[lane]);
                    else DrawDashedLine(g, laneLines[lane], laneLineDists[lane]);
                }
            }
        }

        private void DrawDashedLine(Graphics g, List<PointF> points, List<float> distances)
        {
            if (points == null || distances == null || points.Count < 2 || distances.Count != points.Count) return;

            float dashLen = 48f;
            float gapLen = 48f;
            float cycle = dashLen + gapLen;

            using (var pen = new Pen(Color.White, 2))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Flat;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Flat;

                float phase = distances[0] % cycle;
                if (phase < 0) phase += cycle;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    var a = points[i];
                    var b = points[i + 1];
                    float segPathLen = distances[i + 1] - distances[i];
                    if (segPathLen <= 0) continue;
                    float dx = b.X - a.X;
                    float dy = b.Y - a.Y;

                    float segPos = 0f;
                    while (segPos < segPathLen - 0.001f)
                    {
                        float posInCycle = phase % cycle;
                        if (posInCycle < dashLen)
                        {
                            float canDraw = Math.Min(dashLen - posInCycle, segPathLen - segPos);
                            float t0 = segPos / segPathLen;
                            float t1 = (segPos + canDraw) / segPathLen;
                            var sx = a.X + dx * t0;
                            var sy = a.Y + dy * t0;
                            var ex = a.X + dx * t1;
                            var ey = a.Y + dy * t1;
                            g.DrawLine(pen, sx, sy, ex, ey);
                            segPos += canDraw;
                            phase += canDraw;
                        }
                        else
                        {
                            float skip = Math.Min(cycle - posInCycle, segPathLen - segPos);
                            segPos += skip;
                            phase += skip;
                        }
                    }
                }
            }
        }

        private void DrawDoubleSolidLine(Graphics g, List<PointF> points)
        {
            if (points.Count < 2) return;
            using (var pen1 = new Pen(Color.Yellow, 3))
            using (var pen2 = new Pen(Color.Yellow, 3))
            {
                float offset = 5f;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    float dx = points[i + 1].Y - points[i].Y;
                    float dy = points[i].X - points[i + 1].X;
                    float len = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (len < 0.1f) continue;
                    dx /= len; dy /= len;
                    PointF p1a = new PointF(points[i].X + dx * offset, points[i].Y + dy * offset);
                    PointF p1b = new PointF(points[i].X - dx * offset, points[i].Y - dy * offset);
                    PointF p2a = new PointF(points[i + 1].X + dx * offset, points[i + 1].Y + dy * offset);
                    PointF p2b = new PointF(points[i + 1].X - dx * offset, points[i + 1].Y - dy * offset);
                    g.DrawLine(pen1, p1a, p2a);
                    g.DrawLine(pen2, p1b, p2b);
                }
            }
        }

        private void DrawCenterLine(Graphics g, List<PointF> leftEdge, List<PointF> rightEdge)
        {
            if (leftEdge.Count < 2 || rightEdge.Count < 2) return;

            var points = new PointF[leftEdge.Count];
            for (int i = 0; i < leftEdge.Count && i < rightEdge.Count; i++)
            {
                points[i] = new PointF((leftEdge[i].X + rightEdge[i].X) / 2, (leftEdge[i].Y + rightEdge[i].Y) / 2);
            }

            float playerSpeed = Player.Velocity.Y * (float)Math.Cos(Player.Angle) - Player.Velocity.X * (float)Math.Sin(Player.Angle);
            float playerDistanceOnRoad = -Player.WorldY;
            float playerOffset = playerDistanceOnRoad % 48f;
            if (playerOffset < 0) playerOffset += 48f;
            if (playerSpeed < -0.1f) playerOffset = 48f - playerOffset;
            using (var pen = new Pen(Color.Yellow, 3))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                pen.DashPattern = new float[] { 16, 32 };
                pen.DashOffset = playerOffset;
                pen.DashCap = System.Drawing.Drawing2D.DashCap.Flat;
                g.DrawLines(pen, points);
            }
        }

        private void DrawRoadMarkings(Graphics g, List<PointF> leftEdge, List<PointF> rightEdge)
        {
            if (leftEdge.Count < 2) return;
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
            float size = Math.Max(28f, Player.Width * 0.95f);
            float height = size * (Player.Height / Player.Width);

            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, enemy.WorldX - size / 2 - 4, enemy.WorldY + height / 2, size + 8, Math.Max(6f, height * 0.14f));
            }

            float corner = Math.Min(10f, size * 0.18f);
            var bodyRect = new RectangleF(enemy.WorldX - size / 2, enemy.WorldY - height / 2, size, height);

            using (var bodyPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                bodyPath.AddArc(bodyRect.X, bodyRect.Y, corner * 2, corner * 2, 180, 90);
                bodyPath.AddArc(bodyRect.X + bodyRect.Width - corner * 2, bodyRect.Y, corner * 2, corner * 2, 270, 90);
                bodyPath.AddArc(bodyRect.X + bodyRect.Width - corner * 2, bodyRect.Y + bodyRect.Height - corner * 2, corner * 2, corner * 2, 0, 90);
                bodyPath.AddArc(bodyRect.X, bodyRect.Y + bodyRect.Height - corner * 2, corner * 2, corner * 2, 90, 90);
                bodyPath.CloseFigure();
                using (var brush = new SolidBrush(enemy.Color)) g.FillPath(brush, bodyPath);
                using (var pen = new Pen(Color.Black, 1.6f)) g.DrawPath(pen, bodyPath);
            }

            var winRect = new RectangleF(enemy.WorldX - size * 0.28f, enemy.WorldY - height * 0.28f, size * 0.56f, height * 0.22f);
            using (var windowBrush = new SolidBrush(Color.FromArgb(220, 200, 230, 255)))
            {
                g.FillRectangle(windowBrush, winRect);
                using (var pen = new Pen(Color.LightBlue, 1f)) g.DrawRectangle(pen, winRect.X, winRect.Y, winRect.Width, winRect.Height);
            }

            using (var headBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 200)))
            using (var tailBrush = new SolidBrush(Color.FromArgb(200, 180, 30, 30)))
            {
                g.FillEllipse(headBrush, enemy.WorldX - size * 0.22f, enemy.WorldY - height / 2 - (height * 0.04f), size * 0.12f, height * 0.12f);
                g.FillEllipse(headBrush, enemy.WorldX + size * 0.1f, enemy.WorldY - height / 2 - (height * 0.04f), size * 0.12f, height * 0.12f);
                g.FillEllipse(tailBrush, enemy.WorldX - size * 0.22f, enemy.WorldY + height / 2 - (height * 0.08f), size * 0.12f, height * 0.12f);
                g.FillEllipse(tailBrush, enemy.WorldX + size * 0.1f, enemy.WorldY + height / 2 - (height * 0.08f), size * 0.12f, height * 0.12f);
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

                    using (var brush = new SolidBrush(Color.Red)) g.DrawString(gameOverText, font, brush, x, y);
                }

                using (var font = new Font("Arial", 20))
                {
                    string finalScoreText = $"Final Score: {_score / 60}";
                    SizeF size = g.MeasureString(finalScoreText, font);
                    float x = (screenWidth - size.Width) / 2;
                    float y = (screenHeight - size.Height) / 2 + 60;
                    using (var brush = new SolidBrush(Color.Yellow)) g.DrawString(finalScoreText, font, brush, x, y);
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
            float playerDistanceOnRoad = -Player.WorldY;
            _roadPath.GetWorldPosition(playerDistanceOnRoad, out float roadCenterX, out float roadCenterY, out float roadAngle);
            float dx = Player.WorldX - roadCenterX;
            float dy = Player.WorldY - roadCenterY;
            float perpX = (float)Math.Cos(roadAngle);
            float perpY = (float)Math.Sin(roadAngle);
            float lateralDistance = Math.Abs(dx * perpX + dy * perpY);
            const float roadWidthHalf = RoadWidth / 2;
            return lateralDistance < roadWidthHalf;
        }

        public void Reset()
        {
            Player = new PlayerCar();
            SetPlayerModel(_currentModel);
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

