using System;
using System.Drawing;

namespace TopDownHighwayDrifter
{
    public class EnemyCar : GameObject
    {
        public float Speed { get; set; }
        public Color CarColor { get; set; }

        public EnemyCar(float x, float y, float speed, Color color) 
            : base(x, y, 18, 28)
        {
            Speed = speed;
            CarColor = color;
            Color = color;
        }

        public override void Update(float scrollSpeed)
        {
            Y += scrollSpeed;
        }

        public override void Draw(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Простой прямоугольник с закругленными углами для автомобиля
            using (var brush = new SolidBrush(CarColor))
            {
                g.FillRectangle(brush, X, Y, Width, Height);
            }

            // Окно машины
            using (var windowBrush = new SolidBrush(Color.LightBlue))
            {
                g.FillRectangle(windowBrush, X + 2, Y + 6, Width - 4, 8);
            }

            // Тень под машиной
            using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, X - 2, Y + Height + 2, Width + 4, 5);
            }

            // Контур
            using (var pen = new Pen(Color.Black, 1.5f))
            {
                g.DrawRectangle(pen, X, Y, Width, Height);
            }
        }
    }
}
