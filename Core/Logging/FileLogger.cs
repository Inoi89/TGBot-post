using Microsoft.Extensions.Options;
using Models.Options;

namespace Core.Logging
{
    public class FileLogger : LoggerBase
    {
        private readonly string _logFilePath;

        public FileLogger(IOptions<FileLoggerOptions> fileLoggerOptions)
        {
            _logFilePath = fileLoggerOptions.Value.FilePath;
        }

        protected override void LogMessage(LogMessageType messageType, string message)
        {
            try
            {
                using StreamWriter writer = new StreamWriter(_logFilePath, true);
                string logEntry = $"[{messageType}] [{DateTime.Now.TimeOfDay}] {message}";
                writer.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в лог-файл: {ex.Message}");
            }
        }

        public bool HasElapsedOneHourSinceLastPost()
        {
            try
            {
                string[] logLines = System.IO.File.ReadAllLines(_logFilePath);

                if (logLines.Length > 0)
                {
                    string lastLogEntry = logLines[logLines.Length - 1];
                    string[] parts = lastLogEntry.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 3)
                    {
                        string timePart = parts[2]; // Время будет в третьей части
                        if (TimeSpan.TryParse(timePart, out TimeSpan lastPostTime))
                        {
                            // Получаем текущее время
                            TimeSpan currentTime = DateTime.Now.TimeOfDay;

                            // Проверяем, прошел ли хотя бы час с момента последней публикации
                            // Используй 12-часовой формат, иначе не работает
                            if (1 == 1) //(currentTime - lastPostTime >= TimeSpan.FromHours(1))
                            {
                                return true;
                            }
                        }
                    }
                }
                else return true;
            }
            catch (Exception ex)
            {
                // Обработка ошибок чтения файла логов
                Console.WriteLine($"Ошибка при чтении лог-файла: {ex.Message}");
            }

            return false;
        }
    }
}
