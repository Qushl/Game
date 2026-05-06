using System;
using System.Drawing;
using System.Collections.Generic;

namespace TopDownHighwayDrifter
{
    /// <summary>
    /// Автомобиль игрока на основе мировых координат
    /// </summary>
    public class PlayerCar : GameObject
    {
        // Для резкого заноса при выезде на обочину
        private bool _wasOnRoadLastFrame = true;
        // Мировые координаты (независимые от дороги)
        public float WorldX { get; set; } = 400;
        public float WorldY { get; set; } = 400;
        
        // Угол поворота машины в радианах
        public float Angle { get; set; } = (float)(-Math.PI / 2); 
        
        // Вектор скорости
        public Vector2 Velocity { get; set; } = Vector2.Zero;
        
        // Управление
        public float TurnSpeed { get; set; } = 0.08f;
        public float Acceleration { get; set; } = 0.3f;
        public float MaxSpeed { get; set; } = 10f;
        public float Friction { get; set; } = 0.97f;
        public float DriftFactor { get; set; } = 0.97f;

        // Дрифт-эффекты
        public float DriftIntensity { get; private set; } = 0;
        public List<DriftSmoke> SmokeParticles { get; private set; } = new List<DriftSmoke>();
        
        // Состояние дороги
        public bool IsOnRoad { get; set; } = true;
        private Random _particleRandom = new Random();

        private bool _turningLeft = false;
        private bool _turningRight = false;
        private bool _isAccelerating = false;
        private bool _isBraking = false;

        public PlayerCar() : base(0, 0, 18, 28)
        {
            Color = Color.CornflowerBlue;
            // Тестовая инициализация дыма для проверки
            for (int i = 0; i < 10; i++)
            {
                SmokeParticles.Add(new DriftSmoke
                {
                    X = WorldX,
                    Y = WorldY + i * 5,
                    Life = 1.0f - i * 0.08f,
                    Size = 20 + i * 2,
                    VelocityX = 0,
                    VelocityY = 0
                });
            }
        }

        public void SetInput(bool left, bool right, bool accelerate, bool brake = false)
        {
            _turningLeft = left;
            _turningRight = right;
            _isAccelerating = accelerate;
            _isBraking = brake;
        }

        public override void Update(float scrollSpeed)
        {
            // Эффекты травы 
            float effectiveTurnSpeed = TurnSpeed;
            float effectiveFriction = Friction;
            
            if (!IsOnRoad)
            {
                effectiveTurnSpeed = TurnSpeed * 0.35f; // На траве очень сложно поворачивать
                effectiveFriction = 0.93f; // На траве меньше трения (эффект льда)

                // Резкий занос только при пересечении линии обочины
                if (_wasOnRoadLastFrame)
                {
                    // Случайный угол заноса в пределах [-30, 30] градусов
                    float randomAngle = ((float)_particleRandom.NextDouble() - 0.5f) * 0.9f; // ~50 градусов макс
                    Angle += randomAngle;
                }
            }

            // Обновляем флаг для следующего кадра
            _wasOnRoadLastFrame = IsOnRoad;

            // Управление поворотом
            if (_turningLeft)
                Angle -= effectiveTurnSpeed;
            if (_turningRight)
                Angle += effectiveTurnSpeed;

            // Применяем ограничение угла от -180 до 180 градусов
            while (Angle > Math.PI) Angle -= (float)(2 * Math.PI);
            while (Angle < -Math.PI) Angle += (float)(2 * Math.PI);

            // Вектор направления
            Vector2 forward = new Vector2(
                (float)Math.Cos(Angle),
                (float)Math.Sin(Angle)
            );

            // Ускорение / Тормоз
            if (_isAccelerating)
            {
                Velocity = Velocity + (forward * Acceleration);
            }
            if (_isBraking)
            {
                Velocity = Velocity - (forward * Acceleration * 0.5f);
            }

            // Ограничение скорости
            float speed = Velocity.Length();
            if (speed > MaxSpeed)
            {
                Velocity = Velocity.Normalize() * MaxSpeed;
            }

            // Дрифт - увеличенный, и ещё больше на траве (эффект обочины)
            float effectiveDriftFactor = DriftFactor;
            if (!IsOnRoad)
            {
                effectiveDriftFactor = 0.99f; // Намного сильнее занос на траве
            }

            if (speed > 0.5f)
            {
                Vector2 desiredDirection = forward * speed;
                // За кадр скорость меняется в сторону желаемого направления меньше
                Velocity = Vector2.Lerp(Velocity, desiredDirection, 1 - effectiveDriftFactor);
            }

            // Трение
            Velocity = Velocity * effectiveFriction;

            // Интенсивность заноса
            if (speed > 0.1f)
            {
                Vector2 normalizedVelocity = Velocity.Normalize();
                float dot = normalizedVelocity.DotProduct(forward); // cos
                DriftIntensity = Math.Max(0, 1 - Math.Abs(dot)) * speed / MaxSpeed;
            }
            else
            {
                DriftIntensity = 0;
            }

            // Дым при заносе (белый дым) - только на дороге, при любом дрифте
            if (IsOnRoad && DriftIntensity > 0.05f && speed > 0.5f)
            {
                // Генерируем несколько частиц дыма за кадр для более плотного эффекта
                int smokeCount = (int)(1 + DriftIntensity * 3);
                for (int i = 0; i < smokeCount; i++)
                {
                    var smoke = new DriftSmoke
                    {
                        X = WorldX - forward.X * 18 + (float)(_particleRandom.NextDouble() - 0.5f) * 15,
                        Y = WorldY - forward.Y * 18 + (float)(_particleRandom.NextDouble() - 0.5f) * 15,
                        Life = 1.8f,
                        Size = 18 + DriftIntensity * 28,
                        VelocityX = (float)(_particleRandom.NextDouble() - 0.5f) * 2.5f,
                        VelocityY = (float)(_particleRandom.NextDouble() - 0.5f) * 2.5f,
                        IsGrass = false
                    };
                    SmokeParticles.Add(smoke);
                }
            }

            // Зелёные частицы на траве
            if (!IsOnRoad && speed > 0.5f)
            {
                var grassSmoke = new DriftSmoke
                {
                    X = WorldX - forward.X * 18 + (float)(_particleRandom.NextDouble() - 0.5f) * 20,
                    Y = WorldY - forward.Y * 18 + (float)(_particleRandom.NextDouble() - 0.5f) * 20,
                    Life = 1.2f,
                    Size = 12 + (float)_particleRandom.NextDouble() * 12,
                    VelocityX = (float)(_particleRandom.NextDouble() - 0.5f) * 3.0f,
                    VelocityY = (float)(_particleRandom.NextDouble() - 0.5f) * 3.0f,
                    IsGrass = true
                };
                SmokeParticles.Add(grassSmoke);
            }

            // Обновляем дым
            for (int i = SmokeParticles.Count - 1; i >= 0; i--)
            {
                SmokeParticles[i].Life -= 0.03f;
                SmokeParticles[i].X += SmokeParticles[i].VelocityX;
                SmokeParticles[i].Y += SmokeParticles[i].VelocityY;
                SmokeParticles[i].Size += 0.8f;

                if (SmokeParticles[i].Life <= 0)
                    SmokeParticles.RemoveAt(i);
            }

            // Ограничиваем частицы
            while (SmokeParticles.Count > 250)
                SmokeParticles.RemoveAt(0);

            // Перемещаем машину
            WorldX += Velocity.X;
            WorldY += Velocity.Y;

            // Обновляем базовую позицию 
            X = WorldX;
            Y = WorldY;
        }

        public override void Draw(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Рисуем дым дрифта
            foreach (var smoke in SmokeParticles)
                smoke.Draw(g);


            // Рисуем тень
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, WorldX - Width * 0.3f, WorldY + Height * 0.35f, Width * 0.6f, 8);
            }

            // Геометрия
            float sin = (float)Math.Sin(Angle);
            float cos = (float)Math.Cos(Angle);
            float w = Width, h = Height;

            // Кузов (основной прямоугольник)
            PointF[] body = new PointF[4];
            body[0] = new PointF(WorldX - sin * w / 2 - cos * h / 2, WorldY + cos * w / 2 - sin * h / 2);
            body[1] = new PointF(WorldX + sin * w / 2 - cos * h / 2, WorldY - cos * w / 2 - sin * h / 2);
            body[2] = new PointF(WorldX + sin * w / 2 + cos * h / 2, WorldY - cos * w / 2 + sin * h / 2);
            body[3] = new PointF(WorldX - sin * w / 2 + cos * h / 2, WorldY + cos * w / 2 + sin * h / 2);
            using (var brush = new SolidBrush(Color))
                g.FillPolygon(brush, body);
            using (var pen = new Pen(Color.DarkBlue, 2))
                g.DrawPolygon(pen, body);

            // Крыша (меньше и светлее)
            float roofW = w * 0.6f, roofH = h * 0.4f;
            PointF roofCenter = new PointF(WorldX, WorldY - sin * h * 0.1f);
            PointF[] roof = new PointF[4];
            roof[0] = new PointF(roofCenter.X - sin * roofW / 2 - cos * roofH / 2, roofCenter.Y + cos * roofW / 2 - sin * roofH / 2);
            roof[1] = new PointF(roofCenter.X + sin * roofW / 2 - cos * roofH / 2, roofCenter.Y - cos * roofW / 2 - sin * roofH / 2);
            roof[2] = new PointF(roofCenter.X + sin * roofW / 2 + cos * roofH / 2, roofCenter.Y - cos * roofW / 2 + sin * roofH / 2);
            roof[3] = new PointF(roofCenter.X - sin * roofW / 2 + cos * roofH / 2, roofCenter.Y + cos * roofW / 2 + sin * roofH / 2);
            using (var brush = new SolidBrush(Color.LightSkyBlue))
                g.FillPolygon(brush, roof);
            using (var pen = new Pen(Color.SteelBlue, 1.2f))
                g.DrawPolygon(pen, roof);

            // Окна (два прямоугольника)
            float winW = w * 0.22f, winH = h * 0.18f;
            for (int i = -1; i <= 1; i += 2)
            {
                float wx = roofCenter.X + sin * i * winW * 0.7f;
                float wy = roofCenter.Y - cos * i * winW * 0.7f;
                PointF[] win = new PointF[4];
                win[0] = new PointF(wx - sin * winW / 2 - cos * winH / 2, wy + cos * winW / 2 - sin * winH / 2);
                win[1] = new PointF(wx + sin * winW / 2 - cos * winH / 2, wy - cos * winW / 2 - sin * winH / 2);
                win[2] = new PointF(wx + sin * winW / 2 + cos * winH / 2, wy - cos * winW / 2 + sin * winH / 2);
                win[3] = new PointF(wx - sin * winW / 2 + cos * winH / 2, wy + cos * winW / 2 + sin * winH / 2);
                using (var brush = new SolidBrush(Color.WhiteSmoke))
                    g.FillPolygon(brush, win);
                using (var pen = new Pen(Color.LightBlue, 0.8f))
                    g.DrawPolygon(pen, win);
            }

            // Фары (спереди)
            float fx = WorldX + cos * h / 2, fy = WorldY + sin * h / 2;
            using (var headlightBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 180)))
            {
                g.FillEllipse(headlightBrush, fx - 6 - sin * w * 0.25f, fy - 4 + cos * w * 0.25f, 8, 6);
                g.FillEllipse(headlightBrush, fx - 6 + sin * w * 0.25f, fy - 4 - cos * w * 0.25f, 8, 6);
            }

            // Задние фонари (сзади)
            float bx = WorldX - cos * h / 2, by = WorldY - sin * h / 2;
            using (var tailBrush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(tailBrush, bx - 3 - sin * w * 0.25f, by - 2 + cos * w * 0.25f, 6, 4);
                g.FillEllipse(tailBrush, bx - 3 + sin * w * 0.25f, by - 2 - cos * w * 0.25f, 6, 4);
            }

            // Визуализация дрифта (красное свечение)
            if (DriftIntensity > 0.2f)
            {
                int driftAlpha = Math.Min(120, (int)(DriftIntensity * 180));
                using (var driftBrush = new SolidBrush(Color.FromArgb(driftAlpha, 255, 40, 40)))
                {
                    g.FillPolygon(driftBrush, body);
                }
            }
        }
    }

    public class DriftSmoke
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public bool IsGrass { get; set; } = false;

        public void Draw(Graphics g)
        {
            int alpha = (int)(80 * Life);
            if (alpha < 30) alpha = 30;
            if (alpha > 220) alpha = 220;
            if (alpha > 255) alpha = 255;
            if (alpha < 0) alpha = 0;

            if (IsGrass)
            {
                // Зелёные частицы для травы
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new RectangleF(X - Size / 2, Y - Size / 2, Size, Size),
                    Color.FromArgb(alpha, 100, 200, 100),
                    Color.FromArgb(0, 50, 150, 50),
                    90f))
                {
                    g.FillEllipse(brush, X - Size / 2, Y - Size / 2, Size, Size);
                }
            }
            else
            {
                // Белые частицы для дрифта
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new RectangleF(X - Size / 2, Y - Size / 2, Size, Size),
                    Color.FromArgb(alpha, 240, 240, 240),
                    Color.FromArgb(0, 160, 160, 160),
                    90f))
                {
                    g.FillEllipse(brush, X - Size / 2, Y - Size / 2, Size, Size);
                }
            }
        }

        // ...удалена старая версия Draw(Graphics g)...
    }
}
