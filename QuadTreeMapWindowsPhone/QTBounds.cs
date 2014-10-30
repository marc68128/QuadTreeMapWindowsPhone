using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadTreeMap
{
    public struct QTBounds
    {
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public double Left { get; set; }

        public double MidY { get; set; }
        public double MidX { get; set; }

        public QTBounds(double top, double right, double bottom, double left) : this()
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
            MidY = (Top - Bottom) / 2 + Bottom;
            MidX = (Right - Left) / 2 + Left;
        }

        public bool Intersect(QTBounds bounds)
        {
            return !(Right < bounds.Left || bounds.Right < Left || Top < bounds.Bottom || bounds.Top < Bottom);
        }

        public bool Contains(double y, double x)
        {
            return Bottom <= y && y <= Top && Left <= x && x <= Right;
        }

        public bool Contains(QTBounds bounds)
        {
            return Left <= bounds.Left && bounds.Right <= Right && Bottom < bounds.Bottom && bounds.Top < Top;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(QTBounds))
                return false;

            var bounds = (QTBounds)obj;
            return bounds.Bottom == Bottom && bounds.Left == Left && bounds.Right == Right && bounds.Top == Top;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
