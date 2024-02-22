using Core.Logging;

namespace Infrastructure.Contracts
{
    public interface ILogger
    {
        void Log(LogMessageType messageType, string message);
    }
}
