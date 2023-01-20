using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SimpleDrawing
{
    internal class DrawController
    {
        private readonly Logger logger = new Logger();
        public const int DISTANCE_DRAWING_CHECK = 2;
        public const int DEFAULT_STROKE_THICKNESS = 5;
        private int strokeThickness = DEFAULT_STROKE_THICKNESS;
        private bool isMouseDown = false;
        private bool hasLeft = false;
        (Point? start, Point? end) live = new(null, null);
        Point? mousePos = null;
        Ellipse mouseEllipse = null;
        Brush drawingBrush = new SolidColorBrush(Colors.Red);
        public Canvas Canvas { get; set; }
        public event EventHandler<Canvas> CanvasChanged;
        public event EventHandler<(Line, Color)> LineAdded;
        Line MakeLine(Point a, Point b)
        {
            var line = new Line();
            line.Stroke = drawingBrush;
            line.X1 = a.X;
            line.Y1 = a.Y;
            line.X2 = b.X;
            line.Y2 = b.Y;
            line.StrokeThickness = strokeThickness;
            //line.Height = strokeThickness * 1000;
            return line;
        }
        Ellipse MakeEllipse(Point a)
        {
            var ellipse = new Ellipse
            {
                Width = strokeThickness,
                Height = strokeThickness,
                Fill = drawingBrush
            };
            Canvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, a.X);
            Canvas.SetTop(ellipse, a.Y);
            return ellipse;
        }

        //void MouseMoved()
        //{
        //    if (!mouseAttached)
        //        return;
        //    var mousePos = e.GetPosition(Field);
        //    if (live.end != null && Math.Abs(mousePos.X - live.end.Value.X) < DISTANCE_DRAWING_CHECK && Math.Abs(mousePos.Y - live.end.Value.Y) < DISTANCE_DRAWING_CHECK)
        //        return;
        //    if (!isStartSet)
        //        live.start = e.GetPosition(Field);
        //    else
        //        live.end = e.GetPosition(Field);
        //    isStartSet = !isStartSet;
        //    Debug.WriteLine(e.GetPosition(Field));
        //    if (live.start != null && live.end != null)
        //    {
        //        var line = MakeLine((Point)live.start, (Point)live.end);
        //        tempLines.Add(line);
        //        Field.Children.Add(line);
        //    }
        //    debugLabel.Content = Field.Children.Count;
        //}

        private void DrawEllipseAtCursor()
        {
            if (mousePos == null)
            {
                return;
            }
            if (mouseEllipse != null)
                Canvas.Children.Remove(mouseEllipse);
            var circle = MakeEllipse(mousePos.Value);
            mouseEllipse = circle;
            //Canvas.Children.Add(circle);
        }
        void CheckDrawing()
        {
            if (!isMouseDown || hasLeft)
                return;
            if (mousePos == null)
                return;
            if (live.start == null || (mousePos.Value - live.start.Value).Length < DISTANCE_DRAWING_CHECK)
                return;
            if (mousePos.Value.X >= Canvas.ActualWidth || mousePos.Value.Y >= Canvas.ActualHeight || mousePos.Value.X < 0 || mousePos.Value.Y < 0)
                return;
            var _start = new Point(mousePos.Value.X, mousePos.Value.Y);
            live.end = mousePos;
            var line = MakeLine((Point)live.start, (Point)live.end);
            live.start = _start;
            Canvas.Children.Add(line);
            CanvasChanged?.Invoke(this, Canvas);
            LineAdded?.Invoke(this, (line, ((SolidColorBrush) drawingBrush).Color));
        }

        //void FieldLeft()
        //{
        //    if (!mouseAttached) return;
        //    live.end = e.GetPosition(Field);
        //    isStartSet = !isStartSet;
        //    Debug.WriteLine(e.GetPosition(Field));
        //    if (live.start != null && live.end != null)
        //    {
        //        var line = MakeLine((Point)live.start, (Point)live.end);
        //        tempLines.Add(line);
        //        Field.Children.Add(line);
        //    }
        //    debugLabel.Content = Field.Children.Count;
        //}

        internal void SetCanvasDown(object? sender, Point e)
        {
            logger.Debug("Mouse Down");
            isMouseDown = true;
            live.start = e;
            mousePos = e;
        }

        internal void SetCanvasUp(object? sender, Point e)
        {
            logger.Debug("Mouse Up");
            if (!isMouseDown) return;
            isMouseDown = false;
        }

        internal void MouseMoved(object? sender, Point e)
        {
            mousePos = e;
            //DrawEllipseAtCursor();
            if (!isMouseDown) return;
            logger.Trace("Mouse Moved " + e);
            CheckDrawing();
        }

        internal void MouseLeft(object? sender, (Point e, bool left) state)
        {
            if (state.left)
            {
                hasLeft = true;
                logger.Debug("Mouse Left");
            }
            else
            {
                hasLeft = false;
                logger.Debug("Mouse Entered");
            }
        }

        internal void SetColorChanged(object? sender, Color e)
        {
            ChangeColor(e);
        }

        private void ChangeColor(Color e)
        {
            logger.Trace($"Color changed to {e}");
            drawingBrush = new SolidColorBrush(e);
        }
        internal void ChangeSizeDelta(object? sender, int e)
        {
            logger.Debug("Scroll Delta: " + e);
            if (e > 0)
            {
                if (strokeThickness + 1 <= DEFAULT_STROKE_THICKNESS * 5)
                {
                    strokeThickness++;
                }
            }
            else
            {
                if (strokeThickness - 1 >= DEFAULT_STROKE_THICKNESS)
                {
                    strokeThickness--;
                }
            }
        }

        internal void receiveCommand(object? sender, CommandEventArgs e)
        {
            logger.Debug($"Received command; type: {e.CommandType}, msg: {e.Command}");
            if (e.CommandType != CommandEnum.DRW)
                return;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var coords = e.Command.Split(';');
                var start = new Point(double.Parse(coords[0]), double.Parse(coords[1]));
                var end = new Point(double.Parse(coords[2]), double.Parse(coords[3]));
                var size = int.Parse(coords[4]);
                var color = (Color)ColorConverter.ConvertFromString(coords[5]);
                logger.Trace($"Changed color using command to {color}");
                ChangeColor(color);
                strokeThickness = size;
                var line = MakeLine(start, end);
                Canvas.Children.Add(line);
                CanvasChanged?.Invoke(this, Canvas);
            }));
        }
    }
}
