using System;
using System.Drawing;
using System.Collections.Generic;

namespace TopDownHighwayDrifter
{
    /// <summary>
    /// Автомобиль игрока
    /// </summary>
    public class PlayerCar : GameObject
    {
        public enum CarModelType
        {
            Default,
            Straight,
            Sideways
        }

        public CarModelType Model { get; private set; } = CarModelType.Default;

        public void SetModel(CarModelType model)
        {
            Model = model;
            switch (model)
            {
                case CarModelType.Default:
                    Width = 32;
                    Height = 48;
                    Color = Color.CornflowerBlue;
                    TurnSpeed = 0.09f;
                    Acceleration = 0.30f;
                    MaxSpeed = 10f;
                    Friction = 0.97f;
                    DriftFactor = 0.92f;
                    break;
                case CarModelType.Straight:
                    Width = 30;
                    Height = 46;
                    Color = Color.LightSteelBlue;
                    TurnSpeed = 0.095f;
                    Acceleration = 0.34f;
                    MaxSpeed = 11.5f;
                    Friction = 0.975f;
                    DriftFactor = 0.6f;
                    break;
                case CarModelType.Sideways:
                    Width = 32;
                    Height = 48;
                    Color = Color.MediumPurple;
                    TurnSpeed = 0.09f;
                    Acceleration = 0.30f;
                    MaxSpeed = 10f;
                    Friction = 0.97f;
                    DriftFactor = 0.985f;
                    break;
            }
        }

        private bool _wasOnRoadLastFrame = true;
        public float WorldX { get; set; } = 400;
        public float WorldY { get; set; } = 400;
        public float Angle { get; set; } = (float)(-Math.PI / 2);
        public Vector2 Velocity { get; set; } = Vector2.Zero;
        public float TurnSpeed { get; set; } = 0.08f;
        public float Acceleration { get; set; } = 0.3f;
        public float MaxSpeed { get; set; } = 10f;
        public float Friction { get; set; } = 0.97f;
        public float DriftFactor { get; set; } = 0.97f;
        public float DriftIntensity { get; private set; } = 0;
        public List<DriftSmoke> SmokeParticles { get; private set; } = new List<DriftSmoke>();
        public bool IsOnRoad { get; set; } = true;
        private Random _particleRandom = new Random();

        private bool _turningLeft = false;
        private bool _turningRight = false;
        private bool _isAccelerating = false;
        private bool _isBraking = false;

        public PlayerCar() : base(0, 0, 32, 48)
        {
            SetModel(Model);
            for (int i = 0; i < 10; i++)
            {
                SmokeParticles.Add(new DriftSmoke
                {
                    X = WorldX,
                    Y = WorldY + i * (Height * 0.12f),
                    Life = 1.0f - i * 0.08f,
                    Size = 12 + i * 3,
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
            float effectiveTurnSpeed = TurnSpeed;
            float effectiveFriction = Friction;

            if (!IsOnRoad)
            {
                effectiveTurnSpeed = TurnSpeed * 0.35f;
                effectiveFriction = 0.93f;

                if (_wasOnRoadLastFrame)
                {
                    float randomAngle = ((float)_particleRandom.NextDouble() - 0.5f) * 0.9f;
                    Angle += randomAngle;
                }
            }

            _wasOnRoadLastFrame = IsOnRoad;

            if (_turningLeft) Angle -= effectiveTurnSpeed;
            if (_turningRight) Angle += effectiveTurnSpeed;

            while (Angle > Math.PI) Angle -= (float)(2 * Math.PI);
            while (Angle < -Math.PI) Angle += (float)(2 * Math.PI);

            Vector2 forward = new Vector2((float)Math.Cos(Angle), (float)Math.Sin(Angle));

            if (_isAccelerating)
            {
                Velocity = Velocity + (forward * Acceleration);
            }
            if (_isBraking)
            {
                Velocity = Velocity - (forward * Acceleration * 0.5f);
            }

            float speed = Velocity.Length();
            if (speed > MaxSpeed)
            {
                Velocity = Velocity.Normalize() * MaxSpeed;
            }

            float effectiveDriftFactor = DriftFactor;
            if (!IsOnRoad) effectiveDriftFactor = 0.99f;

            if (speed > 0.5f)
            {
                Vector2 desiredDirection;
                float lerpWeight;

                switch (Model)
                {
                    case CarModelType.Straight:
                        desiredDirection = forward * speed;
                        lerpWeight = Math.Clamp(1 - effectiveDriftFactor + 0.35f, 0.05f, 0.98f);
                        break;
                    case CarModelType.Sideways:
                        desiredDirection = forward * speed;
                        lerpWeight = Math.Clamp(1 - effectiveDriftFactor + 0.05f, 0.02f, 0.7f);
                        break;
                    default:
                        desiredDirection = forward * speed;
                        lerpWeight = Math.Clamp(1 - effectiveDriftFactor, 0.02f, 0.6f);
                        break;
                }

                Velocity = Vector2.Lerp(Velocity, desiredDirection, lerpWeight);
            }

            Velocity = Velocity * effectiveFriction;

            if (speed > 0.1f)
            {
                Vector2 normalizedVelocity = Velocity.Normalize();
                float dot = normalizedVelocity.DotProduct(forward);
                DriftIntensity = Math.Max(0, 1 - Math.Abs(dot)) * speed / MaxSpeed;
            }
            else
            {
                DriftIntensity = 0;
            }

            if (IsOnRoad && DriftIntensity > 0.05f && speed > 0.5f)
            {
                int smokeCount = (int)(1 + DriftIntensity * 3);
                for (int i = 0; i < smokeCount; i++)
                {
                    var smoke = new DriftSmoke
                    {
                        X = WorldX - forward.X * (Height * 0.36f) + (float)(_particleRandom.NextDouble() - 0.5f) * (Width * 0.5f),
                        Y = WorldY - forward.Y * (Height * 0.36f) + (float)(_particleRandom.NextDouble() - 0.5f) * (Width * 0.5f),
                        Life = 1.8f,
                        Size = Math.Max(12f, Height * 0.32f + DriftIntensity * 28f),
                        VelocityX = (float)(_particleRandom.NextDouble() - 0.5f) * 2.5f,
                        VelocityY = (float)(_particleRandom.NextDouble() - 0.5f) * 2.5f,
                        IsGrass = false
                    };
                    SmokeParticles.Add(smoke);
                }
            }

            if (!IsOnRoad && speed > 0.5f)
            {
                var grassSmoke = new DriftSmoke
                {
                    X = WorldX - forward.X * (Height * 0.36f) + (float)(_particleRandom.NextDouble() - 0.5f) * (Width * 0.6f),
                    Y = WorldY - forward.Y * (Height * 0.36f) + (float)(_particleRandom.NextDouble() - 0.5f) * (Width * 0.6f),
                    Life = 1.2f,
                    Size = Math.Max(10f, Height * 0.22f + (float)_particleRandom.NextDouble() * 12f),
                    VelocityX = (float)(_particleRandom.NextDouble() - 0.5f) * 3.0f,
                    VelocityY = (float)(_particleRandom.NextDouble() - 0.5f) * 3.0f,
                    IsGrass = true
                };
                SmokeParticles.Add(grassSmoke);
            }

            for (int i = SmokeParticles.Count - 1; i >= 0; i--)
            {
                SmokeParticles[i].Life -= 0.03f;
                SmokeParticles[i].X += SmokeParticles[i].VelocityX;
                SmokeParticles[i].Y += SmokeParticles[i].VelocityY;
                SmokeParticles[i].Size += 0.8f;

                if (SmokeParticles[i].Life <= 0)
                    SmokeParticles.RemoveAt(i);
            }

            while (SmokeParticles.Count > 250)
                SmokeParticles.RemoveAt(0);

            WorldX += Velocity.X;
            WorldY += Velocity.Y;

            X = WorldX;
            Y = WorldY;
        }

        public override void Draw(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var smoke in SmokeParticles)
                smoke.Draw(g);

            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, WorldX - Width * 0.3f, WorldY + Height * 0.35f, Width * 0.6f, 8);
            }

            float sin = (float)Math.Sin(Angle);
            float cos = (float)Math.Cos(Angle);
            float w = Width, h = Height;

            PointF[] body = new PointF[4];
            body[0] = new PointF(WorldX - sin * w / 2 - cos * h / 2, WorldY + cos * w / 2 - sin * h / 2);
            body[1] = new PointF(WorldX + sin * w / 2 - cos * h / 2, WorldY - cos * w / 2 - sin * h / 2);
            body[2] = new PointF(WorldX + sin * w / 2 + cos * h / 2, WorldY - cos * w / 2 + sin * h / 2);
            body[3] = new PointF(WorldX - sin * w / 2 + cos * h / 2, WorldY + cos * w / 2 + sin * h / 2);
            using (var brush = new SolidBrush(Color))
                g.FillPolygon(brush, body);
            using (var pen = new Pen(Color.DarkBlue, 2))
                g.DrawPolygon(pen, body);

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

            float fx = WorldX + cos * h / 2, fy = WorldY + sin * h / 2;
            using (var headlightBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 180)))
            {
                g.FillEllipse(headlightBrush, fx - 6 - sin * w * 0.25f, fy - 4 + cos * w * 0.25f, 8, 6);
                g.FillEllipse(headlightBrush, fx - 6 + sin * w * 0.25f, fy - 4 - cos * w * 0.25f, 8, 6);
            }

            float bx = WorldX - cos * h / 2, by = WorldY - sin * h / 2;
            if (_isBraking)
            {
                using (var tailBrush = new SolidBrush(Color.FromArgb(255, 255, 40, 40)))
                {
                    g.FillEllipse(tailBrush, bx - 3 - sin * w * 0.25f, by - 2 + cos * w * 0.25f, 7, 5);
                    g.FillEllipse(tailBrush, bx - 3 + sin * w * 0.25f, by - 2 - cos * w * 0.25f, 7, 5);
                }
            }
            else
            {
                using (var tailBrush = new SolidBrush(Color.FromArgb(180, 120, 0, 0)))
                {
                    g.FillEllipse(tailBrush, bx - 3 - sin * w * 0.25f, by - 2 + cos * w * 0.25f, 6, 4);
                    g.FillEllipse(tailBrush, bx - 3 + sin * w * 0.25f, by - 2 - cos * w * 0.25f, 6, 4);
                }
            }

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
    }
}
