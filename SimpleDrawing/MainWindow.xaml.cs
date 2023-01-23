using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
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
using Xceed.Wpf.Toolkit;

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
            PrepareComponents();
            ConnectEventHandlers();
            gameController.JoinGame();
            SetGuessingWord("Pilot");
        }

        private void PrepareComponents()
        {
            drawController.Canvas = Field;
            ColorPicker.StandardColors.Remove(ColorPicker.StandardColors.First(c => c.Name == "Transparent"));
            ColorPicker.StandardColors.Add(new ColorItem(Colors.SaddleBrown, "SaddleBrown"));
        }

        private void ConnectEventHandlers()
        {
            drawController.CanvasChanged += DrawController_CanvasChanged;
            gameController.CommandReceived += drawController.receiveCommand;
            gameController.CommandReceived += ReceiveNonDrawingCommand;
            drawController.LineAdded += DrawController_LineAdded;
            gameController.SendClientInfoMessage += GameController_SendClientInfoMessage;
            gameController.PlayerStateChanged += GameController_PlayerStateChanged;
            gameController.GameStateChanged += GameController_GameStateChanged;
        }

        private void GameController_GameStateChanged(object? sender, Enums.GameStateEnum e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GameStateValue.Content = e;
            }));
        }

        private void GameController_PlayerStateChanged(object? sender, Enums.PlayerStateEnum e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PlayerStateValue.Content = e;
            }));
        }

        private void GameController_SendClientInfoMessage(object? sender, string e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                AddChatMessage($"Server: {e}");
            }));
        }

        private void DrawController_LineAdded(object? sender, (Line data, System.Windows.Media.Color c) line)
        {
            gameController.SendCommand(sender, $"{line.data.X1};{line.data.Y1};{line.data.X2};{line.data.Y2};{line.data.StrokeThickness};{line.c}", CommandEnum.DRAWING);
        }

        private void DrawController_CanvasChanged(object? sender, Canvas e)
        {
            logger.Trace($"Set field to: {e.GetHashCode()}");
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
            drawController.MouseMoved(sender, e.GetPosition(Field));
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
            if(message.StartsWith("--"))
            {
                if (message.Contains("choose,"))
                {
                    gameController.SendCommand(this, message.Split(',')[1], CommandEnum.DRAWER_ACKNOWLEDGEMENT);
                }
                return;
            }
            if (message.Trim().Length == 0) return;
            logger.Debug("Send message: " + message);
            AddChatMessage("Me: " + message);
            gameController.SendCommand(sender, message, CommandEnum.MESSAGE);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            drawController.ChangeSizeDelta(sender, e.Delta);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string f = Directory.GetCurrentDirectory();
            f = f.Substring(0, f.IndexOf("bin"));
            //string s = DateTime.Now.ToString().Replace(':','_').Replace('.', '_').Replace(' ', '_');
            string d = $"{DateTime.Now:dd-hh-mm-ss}";
            string p = $"{f}{d}.png";
            Rect rect = new Rect(Field.RenderSize);
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)rect.Right,
              (int)rect.Bottom, 96d, 96d, PixelFormats.Default);
            rtb.Render(Field);
            //endcode as PNG
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            //save to memory stream
            MemoryStream ms = new();

            pngEncoder.Save(ms);
            ms.Close();
            File.WriteAllBytes(p, ms.ToArray());
            logger.Info("Saved picture");
            //((BitmapImage)Field.Image).Save(p);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            drawController.ClearCanvas();
            gameController.SendCommand(sender, null, CommandEnum.CLEAR);
        }

        internal void ReceiveNonDrawingCommand(object? sender, CommandEventArgs e)
        {
            switch (e.CommandType)
            {
                case CommandEnum.MESSAGE:
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddChatMessage("Other: " + e.Command);
                        }));
                    }
                    break;
                case CommandEnum.ROUND_STARTED:
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SetGuessingWord(e.Command);
                        }));
                        break;
                    }
            }
        }

        private void AddChatMessage(string message)
        {
            chatTextBlock.Text += message + Environment.NewLine;
        }

        private void SetGuessingWord(string word)
        {
            //TODO: Word replacements are for testing only!
            //var _tmp = word;
            //word = new string('_', word.Length);

            word = string.Join(' ', word.ToCharArray());
            //word += $" ({_tmp})";
            GuessWordText.Text = word;
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            logger.Trace($"Changed color to: {e.NewValue.Value}");
            drawController.SetColorChanged(sender, e.NewValue.Value);
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            gameController.SendCommand(this, "", CommandEnum.INTIAL_GAME_REQUEST);
        }
    }
}