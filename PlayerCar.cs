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
        public float Friction { get; set; } = 0.93f;
        public float DriftFactor { get; set; } = 0.92f;

        // Дрифт-эффекты
        public float DriftIntensity { get; private set; } = 0;
        public List<DriftSmoke> SmokeParticles { get; private set; } = new List<DriftSmoke>();

        private bool _turningLeft = false;
        private bool _turningRight = false;
        private bool _isAccelerating = false;
        private bool _isBraking = false;

        public PlayerCar() : base(0, 0, 18, 28)
        {
            Color = Color.CornflowerBlue;
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
            // Управление поворотом
            if (_turningLeft)
                Angle -= TurnSpeed;
            if (_turningRight)
                Angle += TurnSpeed;

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

            // Дрифт / Занос
            if (speed > 0.5f)
            {
                Vector2 desiredDirection = forward * speed;
                // За кадр скорость меняется только на 8% в сторону желаемого направления
                Velocity = Vector2.Lerp(Velocity, desiredDirection, 1 - DriftFactor);
            }

            // Трение
            Velocity = Velocity * Friction;

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

            // Дым при заносе
            if (DriftIntensity > 0.3f && speed > 2f)
            {
                var smoke = new DriftSmoke
                {
                    X = WorldX - forward.X * 15,
                    Y = WorldY - forward.Y * 15,
                    Life = 1.0f,
                    Size = 8 + DriftIntensity * 15,
                    VelocityX = (float)(new Random().NextDouble() - 0.5f) * 1.5f,
                    VelocityY = (float)(new Random().NextDouble() - 0.5f) * 1.5f
                };
                SmokeParticles.Add(smoke);
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
            while (SmokeParticles.Count > 50)
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
            {
                smoke.Draw(g);
            }

            // Рисуем машину
            float sin = (float)Math.Sin(Angle);
            float cos = (float)Math.Cos(Angle);

            PointF front = new PointF(
                WorldX + cos * Height / 2,
                WorldY + sin * Height / 2
            );

            PointF back = new PointF(
                WorldX - cos * Height / 2,
                WorldY - sin * Height / 2
            );

            var carCorners = new PointF[]
            {
                new PointF(front.X - sin * Width / 2, front.Y + cos * Width / 2),
                new PointF(front.X + sin * Width / 2, front.Y - cos * Width / 2),
                new PointF(back.X + sin * Width / 2, back.Y - cos * Width / 2),
                new PointF(back.X - sin * Width / 2, back.Y + cos * Width / 2)
            };

            // Кузов
            using (var brush = new SolidBrush(Color))
            {
                g.FillPolygon(brush, carCorners);
            }

            // Контур
            using (var pen = new Pen(Color.DarkBlue, 2))
            {
                g.DrawPolygon(pen, carCorners);
            }

            // Окно
            using (var windowBrush = new SolidBrush(Color.LightBlue))
            {
                float midX = (carCorners[0].X + carCorners[1].X) / 2;
                float midY = (carCorners[0].Y + carCorners[1].Y) / 2;
                g.FillEllipse(windowBrush, midX - 6, midY - 6, 12, 12);
            }

            // Визуализация дрифта (красное свечение)
            if (DriftIntensity > 0.2f)
            {
                int alpha = (int)(DriftIntensity * 100);
                using (var pen = new Pen(Color.FromArgb(alpha, 255, 0, 0), 2))
                {
                    g.DrawPolygon(pen, carCorners);
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

        public void Draw(Graphics g)
        {
            int alpha = (int)(Life * 150);
            using (var brush = new SolidBrush(Color.FromArgb(alpha, 150, 150, 150)))
            {
                g.FillEllipse(brush, X - Size / 2, Y - Size / 2, Size, Size);
            }
        }
    }
}
