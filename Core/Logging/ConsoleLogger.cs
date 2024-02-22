namespace Core.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        protected override void LogMessage(LogMessageType messageType, string message)
        {
            ConsoleColor color = GetColorForMessageType(messageType);
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{messageType}] [{DateTime.Now.TimeOfDay}] {message}");
            Console.ForegroundColor = originalColor;
        }

        private ConsoleColor GetColorForMessageType(LogMessageType messageType)
        {
            switch (messageType)
            {
                case LogMessageType.Trace:
                    return ConsoleColor.Blue;
                case LogMessageType.Debug:
                    return ConsoleColor.Yellow;
                case LogMessageType.Info:
                    return ConsoleColor.Green;
                case LogMessageType.Error:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.White;
            }
        }
    }
}
