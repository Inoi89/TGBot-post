using Core.Logging;
using Telegram.Bot.Types;
using Infrastructure.Services;
using Telegram.Bot;
using Infrastructure.Contracts;
using Models.Options;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.IO;

namespace Core.Services
{
    public class DavaySimBotService : BotServiceBase
    {
        private readonly IImageService _imageService;

        public DavaySimBotService(IImageService imageService, ILogger logger) : base(logger)
        {
            _imageService = imageService;
        }

        public override async Task ExecuteAsync()
        {
            // Логика выполнения для DavaySimBot
            while (true) // Бесконечный цикл
            {
                // Подгрузка файлов из папки                        
                FileInfo[] imageFiles = _imageService.ProcessImages();

                if (imageFiles.Length == 0)
                {
                    Logger.Log(LogMessageType.Info, "Изображения не найдены");
                }
                else
                {
                    // Проверьте, прошел ли хотя бы час с момента последней публикации
                    if (IsMoscowTimeAllowed()) // fileLogger.HasElapsedOneHourSinceLastPost()
                    {
                        // Прошло достаточно времени, можно продолжить с публикацией изображений
                        Logger.Log(LogMessageType.Info, "Изображения найдены, хуячу дальше.");
                        await ProcessImageFilesAsync(imageFiles);

                        //fileLogger.Log(MessageType.Trace, DateTime.Now.TimeOfDay.ToString());
                    }
                    else
                    {
                        Logger.Log(LogMessageType.Info, "Не время постить");
                    }
                }
                // Задержка перед следующей итерацией
                Random random = new Random();
                int minDelayMinutes = 60; // Минимальная задержка в минутах
                int maxDelayMinutes = 220; // Максимальная задержка в минутах
                int randomDelayMinutes = random.Next(minDelayMinutes, maxDelayMinutes + 1);
                Logger.Log(LogMessageType.Info, $"Хули, ждём {randomDelayMinutes}");
                await Task.Delay(TimeSpan.FromMinutes(randomDelayMinutes));

            }
        }

        // Общее описание прослушки ботом постов
        public override async Task StartRecieving()
        {
            var client = BotClient;
            var path = _imageService.FolderPath;
            

            var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // По умолчанию принимаются все типы обновлений
            };
            BotClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token);
            
            Logger.Log(LogMessageType.Trace, $"Хули, ресивинг фоток в {path} тоже запущен");

        }

        // Метод, в котором бот принимает картиночки в личные сообщения, сохраняет в папочку - после чего удаляет их из переписки
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            Message? message = update.Message;

            // Здесь мы реагируем на посты только в личке
            if (message.Chat.Type != ChatType.Private)
                return;

            if (message.Chat.Type == ChatType.Private && message.Type == MessageType.Photo)
            {
                Logger.Log(LogMessageType.Info, $"Получено сообщение с фотографией");

                var photo = message.Photo.OrderByDescending(p => p.FileSize).FirstOrDefault(); // Выбираем фото с наибольшим размером
                if (photo != null)
                {
                    var fileId = photo.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);
                    var filePath = fileInfo.FilePath;
                    var savePath = Path.Combine(_imageService.FolderPath, $"{fileId}.jpg"); 

                    using (var fileStream = new FileStream(savePath, FileMode.Create))
                    {
                        await botClient.DownloadFileAsync(filePath, fileStream, cancellationToken);
                        Logger.Log(LogMessageType.Info, $"Фотография сохранена по пути: {savePath}");
                    }

                    await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    // Logger.Log(LogMessageType.Debug, $"Удалил из личной переписки");
                }
            }
        }

        // Метод, который берёт картиночки из папочки и постит
        private async Task ProcessImageFilesAsync(FileInfo[] imageFiles)
        {

            Random random = new Random();
            int minImagesToPost = 1; // Минимальное количество изображений для постинга
            int maxImagesToPost = 4; // Максимальное количество изображений для постинга
            FileInfo[] randomImageFiles = imageFiles;

            if (imageFiles.Length > maxImagesToPost)
            {
                Logger.Log(LogMessageType.Debug, "Картинок больше 6");
                // Выбираем случайное количество изображений
                int imagesToPost = random.Next(minImagesToPost, maxImagesToPost + 1);

                // Выбираем случайное подмножество изображений
                randomImageFiles = imageFiles.OrderBy(_ => random.Next()).Take(imagesToPost).ToArray();
            }
            else { Logger.Log(LogMessageType.Debug, "Картинок меньше 6"); }

            foreach (FileInfo imageFile in randomImageFiles)
            {
                // string chatId = botConfig.ChatId;
                using (FileStream stream = new FileStream(imageFile.FullName, FileMode.Open))
                {
                    InputFileStream file = new InputFileStream(stream);
                    string caption = "";

                    Message message = await BotClient.SendPhotoAsync(
                        chatId: BotSettings.ChatId,
                        photo: file,
                        caption: caption
                    );
                }
                Logger.Log(LogMessageType.Trace, "Запостил");

                // Удалить файл после обработки
                imageFile.Delete();
            }
        }

        public static bool IsMoscowTimeAllowed()
        {
            // Устанавливаем информацию о временной зоне для Москвы
            TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

            // Получаем текущее время в Московской временной зоне
            DateTime moscowTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);

            // Устанавливаем граничные часы для проверки времени
            int endHour = 10;   // 10 часов утра

            // Проверяем, находится ли текущее время в указанном диапазоне
            if (moscowTime.Hour < endHour)
            {
                Console.WriteLine($"{moscowTime.Hour} не больше {endHour}");
                return false;  // В этот период времени не постим
            }
            else
            {
                return true;   // В остальное время можно постить
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Logger.Log(LogMessageType.Error, $"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

    }
}
