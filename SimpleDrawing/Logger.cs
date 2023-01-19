using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDrawing
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Error(string message, Exception? ex = null);
    }
    internal class Logger : ILogger
    {
        static readonly log4net.Core.Level traceLevel = new log4net.Core.Level(5, "TRACE");

        private readonly ILog _logger;
        public Logger()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        public void Debug(string message)
        {
            _logger?.Debug(message);
        }
        public void Info(string message)
        {
            _logger?.Info(message);
        }
        public void Error(string message, Exception? ex = null)
        {
            _logger?.Error(message, ex?.InnerException);
        }
        public void Trace(string message, Exception? ex = null)
        {
            _logger?.Logger.Log(MethodBase.GetCurrentMethod()?.DeclaringType, traceLevel, message, ex?.InnerException);
        }
    }
}
