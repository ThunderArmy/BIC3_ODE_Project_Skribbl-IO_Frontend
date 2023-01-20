using log4net.Repository.Hierarchy;
using SimpleDrawing.Service;
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
        private static GameController instance = null;
        private static readonly object padlock = new();
        private TcpService client;
        public event EventHandler<CommandEventArgs> CommandReceived;
        public Task game;
        private GameController() {
            client = new(hostname, port);
            client.MessageReceived += ReceiveCommand;
        }

        private void ReceiveCommand(object? sender, string command)
        {
            string _type = command.Substring(0, 3);
            if (Enum.TryParse(_type, out CommandEnum commandType))
            {
                string _command = command.Substring(3);
                logger.Debug($"Forwarded message; type: {_type}, msg: {_command}");
                CommandReceived?.Invoke(this, new CommandEventArgs(commandType, _command));
            }
        }

        public void SendCommand(object? sender, string message, CommandEnum commandType)
        {
            if (!client.Connected)
                return;
            client.SendCommand(message, commandType);
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
        public void StartGame(string username = "", string sessionId = "")
        {
            game = Task.Run(client.Start);
            //client.Connect();


            //(GameState session, Player player) = sm.JoinGame(username, sessionId);
            //currentGameState = session;
            //currentGameState.OwnPlayer = player;
            //sm.StartGame(); //MOCK
        }
    }
}
