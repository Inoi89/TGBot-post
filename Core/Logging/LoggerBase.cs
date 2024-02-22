using Infrastructure.Contracts;

namespace Core.Logging
{
    public abstract class LoggerBase : ILogger
    {
        protected abstract void LogMessage(LogMessageType messageType, string message);

        public void Log(LogMessageType messageType, string message)
        {
            LogMessage(messageType, message);
        }
    }
}
