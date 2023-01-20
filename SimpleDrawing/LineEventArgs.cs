
using System.Windows;
using System.Windows.Media;

namespace SimpleDrawing
{
    public class LineEventArgs
    {
        public LineEventArgs(int size, Color color, Point start, Point end)
        {
            Size = size;
            Color = color;
            Start = start;
            End = end;
        }

        public int Size { get; }
        public Color Color { get; }
        public Point Start { get; }
        public Point End { get; }
    }
}