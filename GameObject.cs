using System.Drawing;

namespace TopDownHighwayDrifter
{
    public abstract class GameObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Color Color { get; set; }

        public GameObject(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Color = Color.Black;
        }

        public virtual void Update(float scrollSpeed)
        {
            Y += scrollSpeed;
        }

        public virtual void Draw(Graphics g)
        {
            using (var brush = new SolidBrush(Color))
            {
                g.FillRectangle(brush, X, Y, Width, Height);
            }
        }

        public bool IsOffScreen(int screenHeight)
        {
            return Y > screenHeight || Y + Height < 0;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }
    }
}
