using System;
using System.Drawing;

namespace TopDownHighwayDrifter
{
    public class EnemyCar : GameObject
    {
        public float Speed { get; set; }
        public Color CarColor { get; set; }

        public EnemyCar(float x, float y, float speed, Color color) : base(x, y, 18, 28)
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

            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, X + Width / 2 - Width * 0.6f / 2, Y + Height * 0.85f, Width * 0.6f, 7);
            }

            using (var bodyPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                bodyPath.AddArc(X, Y, 8, 8, 180, 90);
                bodyPath.AddArc(X + Width - 8, Y, 8, 8, 270, 90);
                bodyPath.AddArc(X + Width - 8, Y + Height - 8, 8, 8, 0, 90);
                bodyPath.AddArc(X, Y + Height - 8, 8, 8, 90, 90);
                bodyPath.CloseFigure();
                using (var brush = new SolidBrush(CarColor)) g.FillPath(brush, bodyPath);
                using (var pen = new Pen(Color.Black, 1.5f)) g.DrawPath(pen, bodyPath);
            }

            using (var windowBrush = new SolidBrush(Color.LightBlue))
            {
                g.FillRectangle(windowBrush, X + 4, Y + 6, Width - 8, 8);
            }

            using (var headlightBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 180)))
            {
                g.FillEllipse(headlightBrush, X + Width / 2 - 4, Y - 3, 8, 6);
                g.FillEllipse(headlightBrush, X + 3, Y - 2, 5, 4);
                g.FillEllipse(headlightBrush, X + Width - 8, Y - 2, 5, 4);
            }

            using (var tailBrush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(tailBrush, X + 3, Y + Height - 4, 5, 4);
                g.FillEllipse(tailBrush, X + Width - 8, Y + Height - 4, 5, 4);
            }
        }
    }
}
