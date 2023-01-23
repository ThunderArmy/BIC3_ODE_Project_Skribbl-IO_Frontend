using EnumStringValues;
using SimpleDrawing.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleDrawing.Service
{

    class TcpService
    {
        private readonly Logger logger = new Logger();
        private TcpClient Client { get; set; }
        private StreamWriter output = null;
        private StreamReader input = null;
        private TcpStateEnum currentState = TcpStateEnum.DISCONNECTED;
        private readonly string hostname;
        private readonly int port;
        private static readonly object _lock = new();
        public bool Disconnected => currentState == TcpStateEnum.DISCONNECTED;
        public bool Starting => currentState == TcpStateEnum.STARTING;
        public bool Connected => currentState == TcpStateEnum.CONNECTED;
        public event EventHandler<string> DebugMessageSent;
        public event EventHandler<string> MessageReceived;
        public event EventHandler<TcpStateEnum> StateChanged;
        public TcpService(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
        }
        public void SendCommand(CommandEnum commandEnum, string message = "")
        {
            // Das Loglevel von drawing command ist trace!
            if (commandEnum == CommandEnum.DRAWING)
            {
                logger.Trace("Trying to send command connection status: " + currentState);
            }
            else
            {
                logger.Debug("Trying to send command connection status: " + currentState);
            }

            output.WriteLine($"{commandEnum.GetStringValue()}{message}");

            if (commandEnum == CommandEnum.DRAWING)
            {
                logger.Trace("Command:  " + commandEnum + " with message: " + message + " sent. ");
            }
            else
            {
                logger.Debug("Command:  " + commandEnum + " with message: " + message + " sent. ");

            }
        }
        public Task Start()
        {
            return new Task(() =>
            {
                Connect();
            });
        }
        public void Connect()
        {
            logger.Info("Starting client ...");
            logger.Debug("Current connection status: " + currentState);

            lock (_lock)
            {
                if (currentState != TcpStateEnum.DISCONNECTED)
                {
                    logger.Info("Client is in another state other than disconnected!");
                    return;
                }
                currentState = TcpStateEnum.STARTING;
                StateChanged?.Invoke(this, currentState);
            }

            try
            {
                do
                {
                    listenFromServer();
                } while (true);
            }
            catch (NumberOfRetriesExceededException e)
            {
                logger.Info("Max number of tries reached.");
                DebugMessageSent?.Invoke(this, "Verbindung mit Server ist fehlgeschlagen!");
                lock (_lock)
                {
                    currentState = TcpStateEnum.DISCONNECTED;
                    StateChanged?.Invoke(this, currentState);
                }

            }
        }

        public void listenFromServer()
        {
            try
            {
                if (currentState == TcpStateEnum.STARTING)
                {
                    logger.Info("Client establishing connection ...");
                    Client = new TcpClient(hostname, port);
                    output = new StreamWriter(Client.GetStream(), Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    input = new StreamReader(Client.GetStream(), Encoding.UTF8);

                    logger.Info("Client connected successfully!");

                    lock (_lock)
                    {
                        currentState = TcpStateEnum.CONNECTED;
                        StateChanged?.Invoke(this, currentState);
                    }
                }

                do
                {
                    string message = input.ReadLine();
                    logger.Trace("Received message: " + message);
                    MessageReceived?.Invoke(this, message);
                } while (true);
            }
            catch (SystemException e)
            {
                DebugMessageSent?.Invoke(this, "Server disconnected, while receiving message!");
                logger.Error("Server disconnected, while receiving message!");
                lock (_lock)
                {
                    currentState = TcpStateEnum.ERROR;
                    StateChanged?.Invoke(this, currentState);
                }
                disconnect();
                retry();
            }
        }
        public void retry()
        {
            int tryNum = 0;
            double secondsToWait = 1;

            logger.Info("Trying to reconnect to server ...");

            lock (_lock)
            {
                currentState = TcpStateEnum.RETRYING;
                StateChanged?.Invoke(this, currentState);
            }
            do
            {
                try
                {
                    secondsToWait = Math.Min(60, Math.Pow(2, tryNum));
                    tryNum++;
                    logger.Info($"Retry {tryNum} connecting to server after sleep {secondsToWait}");
                    StateChanged?.Invoke(this, currentState);

                    Thread.Sleep((int)secondsToWait * 1000);
                    Client = new TcpClient(hostname, port);
                    output = new StreamWriter(Client.GetStream(), Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    input = new StreamReader(Client.GetStream(), Encoding.UTF8);
                    logger.Info("Server connection established.");
                    break;
                }
                catch (SocketException e)
                {
                    logger.Error("Connection retry failed.");
                    if (tryNum >= 4)
                    {
                        throw new NumberOfRetriesExceededException();
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    throw new InvalidOperationException(e.Message);
                }
            } while (true);

            logger.Info("Finished reestablishing server connection.");

            lock(_lock) {
                currentState = TcpStateEnum.CONNECTED;
                StateChanged?.Invoke(this, currentState);
            }
        }
        public void disconnect()
        {
            //Wird benutzt falls man kein Retry haben will
            /*
                    lock(_lock) {
                        currentState = TcpStateEnum.DISCONNETED;
                    }
            */

            try
            {
                logger.Info("Try disconnecting from server.");
                if (input != null)
                {
                    input.Close();
                }
                if (output != null)
                {
                    output.Close();
                }
                if(Client != null)
                {
                    Client.Close();
                }
            }
            catch (SocketException e)
            {
                logger.Error($"Error received when trying to disconnect: {e}");
            }
        }

    }
    public class NumberOfRetriesExceededException : Exception
    {
        private const string defaultErrorMessage = "Maximum number of retries exceeded.";
        public NumberOfRetriesExceededException(string message = defaultErrorMessage) : base(message) { }
    }
}
