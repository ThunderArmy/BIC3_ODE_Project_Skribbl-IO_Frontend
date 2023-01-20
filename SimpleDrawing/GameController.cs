using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleDrawing
{
    public enum CommandEnum
    {
        MSG, //Message
        DRW, //DRAW
    }
    public class Player
    {
        static int nextId = 0;
        public int Id { get; private set; }
        public string Name { get; }

        public Player(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = Interlocked.Increment(ref nextId);
        }

        public Player(int id, string name) //REMOVE 
        {
            Id = id;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is Player player &&
                   Id == player.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
    public sealed class GameController
    {
        private readonly Logger logger = new Logger();
        private const int port = 8080;
        private const string hostname = "127.0.0.1";
        public TcpClient tcpClient;
        private static GameController instance = null;
        private static readonly object padlock = new object();
        private StreamWriter tcpWriter = null;
        private StreamReader tcpReader = null;
        private Task recieveTask;
        public event EventHandler<CommandEventArgs> commandReceived;

        //public event EventHandler<(CommandEnum commandType, string message)> CommandReceived;
        private GameController() {
        }

        public void SendCommand(object? sender, (CommandEnum commandType, string message) input)
        {
            logger.Info("Send message: " + input.message);
            if (tcpWriter == null)
                return;
            tcpWriter.WriteLine($"{input.commandType}{input.message}");
        }

        public static GameController Instance
        {
            get
            {
                lock (padlock)
                {
                    instance ??= new GameController();
                    return instance;
                }
            }
        }

        GameState currentGameState = new GameState("Uhr", 60, GameStatusEnum.Drawing, new() { new Player(0, "Herbert"), new Player(1, "Kevin") }, new Player(1, "Kevin"), 1, 3);
        public void JoinGame(string username, string sessionId = "")
        {
            //(GameState session, Player player) = sm.JoinGame(username, sessionId);
            //currentGameState = session;
            //currentGameState.OwnPlayer = player;
            //sm.StartGame(); //MOCK
        }
        private void ConnectToServer()
        {
            logger.Info("Connecting to server");
            tcpClient = new TcpClient(hostname, port);
            tcpWriter = new StreamWriter(tcpClient.GetStream(), Encoding.UTF8)
            {
                AutoFlush = true
            };
            recieveTask = Task.Run(async () =>
            {
                //tcpReader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8);
                //do
                //{
                //    //tcpReader.ReadLineAsync().ContinueWith(t =>
                //    //{
                //    //    Debug.WriteLine($"Received Message async: {t.Result}");
                //    //});
                //    logger.Debug("Started tcp reader");
                //    var message = await tcpReader.ReadLine();
                //} while (true);
                using (tcpReader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8))
                {
                    string line;
                    while ((line = tcpReader.ReadLine()) != null)
                    {
                        Debug.WriteLine($"Received Message async: {line}");
                        ParseCommand(line);
                        // do something with line
                    }
                }
            });

            //tcpReader.ReadLineAsync().ContinueWith(recieveMessage);
        }
        private void ParseCommand(string command)
        {
            string _type = command.Substring(0, 3);
            if(Enum.TryParse(_type, out CommandEnum commandType))
            {
                string _command = command.Substring(3);
                logger.Debug($"Forwarded message; type: {_type}, msg: {_command}");
                commandReceived?.Invoke(this, new CommandEventArgs(commandType, _command));
            }
        }
        public void StartGame()
        {
            ConnectToServer();
        }
    }
}
