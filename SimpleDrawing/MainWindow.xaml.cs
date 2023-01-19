using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleDrawing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary
    public partial class MainWindow : Window
    {
        private GameController gameController = GameController.Instance;
        private readonly Logger logger = new Logger();
        private DrawController drawController = new();
        public MainWindow()
        {
            logger.Info("Initializing Game Main Window");
            InitializeComponent();
            gameController.StartGame();
            drawController.Canvas = Field;
            drawController.CanvasChanged += DrawController_CanvasChanged;
        }
        private void DrawController_CanvasChanged(object? sender, Canvas e)
        {
            logger.Debug($"Set field to: {e.GetHashCode()}");
            Field = e;
            debugLabel.Content = e.Children.Count;
        }

        private void CanvasDown(object sender, MouseButtonEventArgs e)
        {
            drawController.SetCanvasDown(sender, e.GetPosition(Field));
        }

        private void CanvasUp(object sender, MouseButtonEventArgs e)
        {
            drawController.SetCanvasUp(this, e.GetPosition(Field));
        }

        private void Field_MouseMove(object sender, MouseEventArgs e)
        {
            //logger.Trace("Relative to window " + e.GetPosition(this));
            //logger.Trace("Relative to canvas " + e.GetPosition(Field));
            drawController.MouseMoved(sender, e.GetPosition(Field));
        }

        private void ColorClick(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton b)
            {
                if (sender != Red)
                    Red.IsChecked = false;
                if (sender != Blue)
                    Blue.IsChecked = false;
                if (sender != Green)
                    Green.IsChecked = false;
                if (sender != Yellow)
                    Yellow.IsChecked = false;
                var color = b.Content switch
                {
                    "Red" => Colors.Red,
                    "Green" => Colors.Green,
                    "Blue" => Colors.Blue,
                    "Yellow" => Colors.Yellow,
                    _ => Colors.Red,
                };
                drawController.SetColorChanged(sender, color);
            }
        }

        private void Field_MouseLeave(object sender, MouseEventArgs e)
        {
            drawController.MouseLeft(sender, (e.GetPosition(Field), true));
        }

        private void Field_MouseEnter(object sender, MouseEventArgs e)
        {
            drawController.MouseLeft(sender, (e.GetPosition(Field), false));
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawController.SetCanvasUp(sender, e.GetPosition(Field));
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawController.SetCanvasUp(sender, e.GetPosition(Field));
        }

        private void Button_SendMessage(object sender, RoutedEventArgs e)
        {
            SendMessage(sender, ChatInput.Text);
            ChatInput.Text = string.Empty;

        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Trace("KeyInput: " + e.Key.ToString());
            if (e.Key == Key.Enter)
            {
                SendMessage(sender, ChatInput.Text);
                ChatInput.Text = string.Empty;
            }
        }

        private void SendMessage(object sender, string message)
        {
            if (message.Trim().Length == 0) return;
            logger.Debug("Send message: " + message);
            gameController.SendCommand(sender, (CommandEnum.MSG, message));
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            drawController.ChangeSizeDelta(sender, e.Delta);
        }
    }
}