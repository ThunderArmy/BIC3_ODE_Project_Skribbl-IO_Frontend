using System;

namespace SimpleDrawing
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEnum CommandType { get; }
        public string Command { get; }

        public CommandEventArgs(CommandEnum commandType, string message)
        {
            this.CommandType = commandType;
            this.Command = message;
        }
    }
}