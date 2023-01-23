using EnumStringValues;
using log4net.Repository.Hierarchy;
using SimpleDrawing.Enums;
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
using System.Windows.Interop;

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
        private GameStateEnum gameState = GameStateEnum.INITIAL;
        private PlayerStateEnum playerState = PlayerStateEnum.NONE;

        public event EventHandler<CommandEventArgs> CommandReceived;
        public event EventHandler<GameStateEnum> GameStateChanged;
        public event EventHandler<PlayerStateEnum> PlayerStateChanged;
        public event EventHandler ResetMode;



        private List<string> words;
        public Task game;
        private string chosenWord;
        private bool resetOccured = false;

        public bool IsDrawer { get => playerState == PlayerStateEnum.DRAWER; }
        public bool IsGuesser { get => playerState == PlayerStateEnum.GUESSER; }
        public bool IsInitial { get => gameState == GameStateEnum.INITIAL; }
        public bool IsStarting { get => gameState == GameStateEnum.STARTING; }
        public bool IsFinished { get => gameState == GameStateEnum.FINISHED; }
        private GameController()
        {
            client = new(hostname, port);
            client.MessageReceived += ReceiveCommand;
        }

        private void ReceiveCommand(object? sender, string command)
        {
            //Handle Commands; Commands always have a length of 3
            resetOccured = false;
            string _type = command.Substring(0, 3);
            logger.Debug($"Parsing Command: {_type}");
            if (EnumExtensions.TryParseStringValueToEnum<CommandEnum>(_type, out CommandEnum commandType))
            {
                string _body = command.Substring(3);
                logger.Debug($"Forwarding message; type: {commandType}, msg: {_body}");
                switch (commandType)
                {
                    case CommandEnum.MESSAGE:
                    case CommandEnum.DRAWING:
                    case CommandEnum.CLEAR:
                        CommandReceived?.Invoke(this, new CommandEventArgs(commandType, _body));
                        break;
                    //First state => If nothing has started yet, here checks for incorrect states are being proceeded
                    case CommandEnum.START_GAME_REQUEST:
                        {
                            logger.Info("Start request for the game ...");
                            if (gameState != GameStateEnum.INITIAL)
                            {
                                logger.Info("Client is in another game state other than initial!");
                                logger.Info("Send start request not acknowledged.");
                                client.SendCommand(CommandEnum.START_GAME_NOTACKNOWLEDGEMENT);
                                return;
                            }
                            logger.Info("Send start request acknowledged.");
                            client.SendCommand(CommandEnum.START_GAME_ACKNOWLEDGEMENT);

                            gameState = GameStateEnum.STARTING;
                            GameStateChanged?.Invoke(this, gameState);
                            break;
                        }
                    // All clients have acknowledged, hence the server determined who the drawers and guessers are
                    // Guesser simply wait for the start of round or an abort
                    case CommandEnum.GUESSER_REQUEST:
                        {
                            logger.Info("Trying to set client to guesser mode");
                            logger.Debug("Game state: " + gameState + " Player state: " + playerState);

                            if (gameState != GameStateEnum.STARTING || playerState != PlayerStateEnum.NONE)
                            {
                                logger.Info("Client is in another game or player state!");
                                return;
                            }

                            playerState = PlayerStateEnum.GUESSER;
                            logger.Info("Set client to guesser mode");
                            PlayerStateChanged?.Invoke(this, playerState);
                            CommandReceived?.Invoke(this, new CommandEventArgs(commandType, "You are a guesser. Try to guess the drawn word."));
                            //TODO: Talk to erik about the following code
                            //gameObserver.setGuesserMode();
                            break;
                        }
                    // Drawer gets send a list of 3 words
                    // He then has to choose one of them
                    case CommandEnum.DRAWER_REQUEST:
                        {
                            logger.Info("Trying to set client to drawer mode");
                            logger.Debug($"Game state: {gameState} Player state: {playerState}");

                            if (gameState != GameStateEnum.STARTING || playerState != PlayerStateEnum.NONE)
                            {
                                logger.Info("Client is in another game or player state!");
                                return;
                            }

                            logger.Debug($"Received words: {_body}");
                            string[] split = _body.Substring(3).Split(";");
                            words = split.ToList();

                            playerState = PlayerStateEnum.DRAWER;
                            PlayerStateChanged?.Invoke(this, playerState);

                            CommandReceived?.Invoke(this, new CommandEventArgs(commandType, $"You are the drawer choose a word: {_body}"));
                            //TODO: Potentially add another eventhandler for admin commands; gameObserver.setChoosableWords(words.get(0), words.get(1), words.get(2));

                            //TODO: Talk to erik about the following code
                            //logger.Info("Set client to guesser mode for word choosing");
                            //gameObserver.setGuesserMode();
                            break;
                        }
                    // As the drawer selected a word, the server requests the start of the round
                    case CommandEnum.ROUND_START_REQUEST:
                        {
                            logger.Info("Request to start the round ");
                            logger.Debug("Game state: " + gameState + " Player state: " + playerState);
                            if (IsStarting || !(IsDrawer || IsGuesser))
                            {
                                logger.Info("Client is in another game or player state!");
                                client.SendCommand(CommandEnum.ROUND_START_NOTACKNOWLEDGEMENT);
                                return;
                            }
                            logger.Info("All seems ready to rumble!");
                            gameState = GameStateEnum.STARTED;
                            GameStateChanged?.Invoke(this, gameState);
                            client.SendCommand(CommandEnum.ROUND_START_ACKNOWLEDGEMENT);
                            break;
                        }
                    // Alle haben den Rundenstart Acknowledged, somit erhalten sie das "Wort"
                    // Guesser erhalten Unterstriche("_") als Wort, während der Drawer das eigentliche
                    // Wort erhält
                    case CommandEnum.ROUND_STARTED:
                        {
                            logger.Info("Round start was sent.");
                            logger.Debug("Game state: " + gameState + " Player state: " + playerState);

                            if (gameState != GameStateEnum.STARTED)
                            {
                                logger.Error("Client is in another game!");
                                return;
                            }

                            if (IsDrawer)
                            {
                                //gameObserver.setDrawerMode(); TODO: Set drawer mode
                            }

                            chosenWord = _body;
                            //gameObserver.setDisplayWord(chosenWord);
                            break;
                        }

                    case CommandEnum.CLOSE_GUESS:
                        {
                            //TODO: Set output words gameObserver.outputWords(message.substring(3) + " is close!");
                            break;
                        }

                    case CommandEnum.CORRECT_GUESS:
                        {
                            logger.Info("Word was guessed correctly.");
                            logger.Debug("Game state: " + gameState + " Player state: " + playerState);

                            gameState = GameStateEnum.FINISHED;
                            GameStateChanged?.Invoke(this, gameState);

                            //TODO: Set Win Mode gameObserver.setWinMode();
                            break;
                        }

                    case CommandEnum.ROUND_END_SUCCESS:
                        {
                            if ((IsGuesser && IsFinished) || IsDrawer)
                            {

                                playerState = PlayerStateEnum.NONE;
                                PlayerStateChanged?.Invoke(this, playerState);

                                gameState = GameStateEnum.INITIAL;
                                GameStateChanged?.Invoke(this, gameState);

                                //gameObserver.clearCanvas();
                                //gameObserver.clearText();
                                client.SendCommand(CommandEnum.ROUND_END_ACKNOWLEDGEMENT);
                            }
                            break;
                        }
                    case CommandEnum.ERROR:
                        {
                            logger.Error("An error has occurred.");
                            logger.Debug("Game state: " + gameState + " Player state: " + playerState);
                            Reset();
                            break;
                        }

                }
            }
            else
            {
                logger.Debug("$Parsing of command failed!");
            }
        }

        public void SendCommand(object? sender, string message, CommandEnum commandType)
        {
            if (!client.Connected)
                return;
            client.SendCommand(commandType, message);
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
        public void JoinGame(string username = "", string sessionId = "")
        {
            var thread = new Thread(() =>
            {
                client.Connect();
            });
            thread.IsBackground = true;
            thread.Start();
            //game = Task.Run(client.Start);
            //client.Connect();


            //(GameState session, Player player) = sm.JoinGame(username, sessionId);
            //currentGameState = session;
            //currentGameState.OwnPlayer = player;
            //sm.StartGame(); //MOCK
        }
        public void StartGame()
        {
            if (client.Connected)
            {
                logger.Info("Trying to start a game. Sending a start game request.");
                client.SendCommand(CommandEnum.START_GAME_REQUEST);
            }
        }
        public void DrawerAcknowledge(string word)
        {
            if (!client.Connected || IsDrawer || IsStarting)
            {
                logger.Info($"Current status isConnected: {client.Connected} isDrawer: {IsDrawer} isStarting: {IsStarting}");
                return;
            }
            logger.Info("Drawer chose word and send acknowledgement.");
            client.SendCommand(CommandEnum.DRAWER_ACKNOWLEDGEMENT, word);
        }
        public void Reset()
        {
            logger.Error("Resetting to initial mode.");
            gameState = GameStateEnum.INITIAL;
            GameStateChanged?.Invoke(this, gameState);
            playerState = PlayerStateEnum.NONE;
            PlayerStateChanged?.Invoke(this, playerState);
            words.Clear();
            chosenWord = "";
            ResetMode?.Invoke(this, EventArgs.Empty);
        }
        //TODO: Connect event handlers
        public void OnDebugMessageSent(object sender, string message)
        {
            // Debug Messages only get sent when something causes TCP server problems
            // Therefor we preemptively reset the Game Service and 
            //TODO: Output using message command; gameObserver.outputWords(message);
            if (!resetOccured)
            {
                resetOccured = true;
                Reset();
            }
        }
    }
}
