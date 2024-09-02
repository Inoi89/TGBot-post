using Core.Logging;
using Telegram.Bot.Types;
using Infrastructure.Services;
using Telegram.Bot;
using Infrastructure.Contracts;
using Models.Options;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using Telegram.Bot.Exceptions;

namespace Core.Services
{
    public class DavaySimBotService : BotServiceBase
    {
        private readonly IImageService _imageService;

        // Либо я тупой, либо ебаный телеграм апи не отдаёт сообщения по айди
        // Поэтому для обработки, их приходится где-то хранить
        private Dictionary<long, Message> _messageStorage = new Dictionary<long, Message>();
        public DavaySimBotService(IImageService imageService, ILogger logger) : base(logger)
        {
            _imageService = imageService;
        }

        public override async Task ExecuteAsync()
        {
            while (true)
            {
                FileInfo[] imageFiles = _imageService.ProcessImages();

                if (imageFiles.Length == 0)
                {
                    Logger.Log(LogMessageType.Info, "Изображения не найдены");
                }
                else
                {
                    if (IsMoscowTimeAllowed())
                    {
                        Logger.Log(LogMessageType.Info, "Изображения найдены, хуячу дальше.");
                        await ProcessImageFilesAsync(imageFiles);
                    }
                    else
                    {
                        Logger.Log(LogMessageType.Info, "Не время постить");
                    }
                }

                Random random = new Random();
                int minDelayMinutes = 60;
                int maxDelayMinutes = 220;
                int randomDelayMinutes = random.Next(minDelayMinutes, maxDelayMinutes + 1);
                Logger.Log(LogMessageType.Info, $"Хули, ждём {randomDelayMinutes}");
                await Task.Delay(TimeSpan.FromMinutes(randomDelayMinutes));
            }
        }

        public override async Task StartRecieving()
        {
            var client = BotClient;
            var path = _imageService.FolderPath;

            var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            BotClient.StartReceiving(
                async (botClient, update, cancellationToken) =>
                {
                    if (update.Type == UpdateType.Message)
                    {
                        await HandleUpdateAsync(botClient, update, cancellationToken);
                    }
                    else if (update.Type == UpdateType.CallbackQuery)
                    {
                        await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
                    }
                },
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            Logger.Log(LogMessageType.Trace, $"Ресивинг фоток в {path} тоже запущен");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            Message? message = update.Message;

            if (message.Chat.Type != ChatType.Private)
                return;

            long userId = message.From.Id;

            if (message.Type == MessageType.Text && message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет, я бот канала @davaySim! Я сохраняю мемасики. " +
                    "Мне их, собственно, можно скидывать, ес чо, я их пересылаю с кнопочками аппрувнуть - или нет. Потом они попадают в общий пул, и оттуда я же их когда-нибудь и запосчу. " +
                    "Пока я не умею сохранять подписи к картинке, распознаю только картинку и видосик, а комментарий как текст - идёт отдельным сообщением, и я не умею их совмещать и сохранять. Пока что! " +
                    "Такие дела, жду смешные картиночки получается. ", cancellationToken: cancellationToken);
                Logger.Log(LogMessageType.Info, $"Приветственное сообщение отправлено пользователю {userId}");
                return; 
            }

            if (userId == 34999765)
            {
                await HandleUserMessageAsync(botClient, message, cancellationToken);
            }
            else
            {
                Logger.Log(LogMessageType.Info, $"Получено сообщение от другого пользователя (ID: {userId})");
                var forwardedMessage = await botClient.ForwardMessageAsync(chatId: 34999765, fromChatId: message.Chat.Id, messageId: message.MessageId, cancellationToken: cancellationToken);

                // Сохраняем в ебаный стораж
                _messageStorage[forwardedMessage.MessageId] = forwardedMessage;

                await botClient.SendTextMessageAsync(
                    chatId: 34999765,
                    text: "Одобрить или отклонить сообщение?",
                    replyMarkup: GetApprovalKeyboard(forwardedMessage.MessageId),
                    cancellationToken: cancellationToken
                );
                Logger.Log(LogMessageType.Info, "Сообщение переслано мне с кнопками одобрения");
            }
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var callbackData = callbackQuery.Data;
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Обработка...", cancellationToken: cancellationToken);

            if (callbackData.StartsWith("approve_"))
            {
                var messageId = int.Parse(callbackData.Split('_')[1]);

                if (_messageStorage.TryGetValue(messageId, out var message))
                {
                    await HandleUserMessageAsync(botClient, message, cancellationToken);
                    _messageStorage.Remove(messageId);
                }
                else
                {
                    Logger.Log(LogMessageType.Info, $"Сообщение с ID {messageId} не найдено в хранилище.");
                }

                Logger.Log(LogMessageType.Info, "Сообщение одобрено и сохранено.");
            }
            else if (callbackData.StartsWith("reject_"))
            {
                var messageId = int.Parse(callbackData.Split('_')[1]);
                var chatId = callbackQuery.Message.Chat.Id;

                try
                {
                    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken); // Удаляем оригинальное сообщение
                    _messageStorage.Remove(messageId);
                    Logger.Log(LogMessageType.Info, "Сообщение отклонено и удалено.");
                }
                catch (ApiRequestException ex)
                {
                    Logger.Log(LogMessageType.Error, $"Ошибка при удалении сообщения: {ex.Message}");
                }
            }

            try
            {
                await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                Logger.Log(LogMessageType.Error, $"Ошибка при удалении сообщения с кнопками: {ex.Message}");
            }
        }


        private InlineKeyboardMarkup GetApprovalKeyboard(int messageId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "✅ Одобрить", callbackData: $"approve_{messageId}"),
                    InlineKeyboardButton.WithCallbackData(text: "❌ Отклонить", callbackData: $"reject_{messageId}")
                }
            });
        }

        private async Task HandleUserMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            switch (message.Type)
            {
                case MessageType.Photo:
                    Logger.Log(LogMessageType.Info, "Получено сообщение с фотографией от меня");
                    var photo = message.Photo.OrderByDescending(p => p.FileSize).FirstOrDefault();
                    if (photo != null)
                    {
                        await SavePhotoAsync(botClient, photo, _imageService.FolderPath, cancellationToken);
                    }
                    break;

                case MessageType.Video:
                    Logger.Log(LogMessageType.Info, "Получено сообщение с видео от меня");
                    await SaveVideoAsync(botClient, message.Video, _imageService.FolderPath, cancellationToken);
                    break;

                case MessageType.Text:
                    Logger.Log(LogMessageType.Info, "Получено текстовое сообщение от меня");
                    Console.WriteLine($"Текст от вас: {message.Text}");
                    break;

                default:
                    Logger.Log(LogMessageType.Info, $"Получен неподдерживаемый тип сообщения от меня: {message.Type}");
                    break;
            }

            try
            {
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken);
                Logger.Log(LogMessageType.Info, "Сообщение успешно удалено.");
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message to delete not found"))
            {
                Logger.Log(LogMessageType.Error, "Сообщение не найдено для удаления, возможно, оно уже было удалено.");
            }
        }

        private async Task ProcessImageFilesAsync(FileInfo[] imageFiles)
        {
            Random random = new Random();
            int minImagesToPost = 1;
            int maxImagesToPost = 4;
            FileInfo[] randomImageFiles = imageFiles;

            if (imageFiles.Length > maxImagesToPost)
            {
                Logger.Log(LogMessageType.Debug, "Картинок больше 6");
                int imagesToPost = random.Next(minImagesToPost, maxImagesToPost + 1);
                randomImageFiles = imageFiles.OrderBy(_ => random.Next()).Take(imagesToPost).ToArray();
            }
            else
            {
                Logger.Log(LogMessageType.Debug, "Картинок меньше 6");
                return;
            }

            foreach (FileInfo imageFile in randomImageFiles)
            {
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

                imageFile.Delete();
            }
        }

        private async Task SavePhotoAsync(ITelegramBotClient botClient, PhotoSize photo, string folderPath, CancellationToken cancellationToken)
        {
            var fileId = photo.FileId;
            var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);
            var filePath = fileInfo.FilePath;
            var savePath = Path.Combine(folderPath, $"{fileId}.jpg");

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await botClient.DownloadFileAsync(filePath, fileStream, cancellationToken);
                Logger.Log(LogMessageType.Info, $"Фотография сохранена по пути: {savePath}");
            }
        }

        private async Task SaveVideoAsync(ITelegramBotClient botClient, Video video, string folderPath, CancellationToken cancellationToken)
        {
            var fileId = video.FileId;
            var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);
            var filePath = fileInfo.FilePath;
            var savePath = Path.Combine(folderPath, $"{fileId}.mp4");

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await botClient.DownloadFileAsync(filePath, fileStream, cancellationToken);
                Logger.Log(LogMessageType.Info, $"Видео сохранено по пути: {savePath}");
            }
        }

        public static bool IsMoscowTimeAllowed()
        {
            TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime moscowTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);
            int endHour = 10;

            if (moscowTime.Hour < endHour)
            {
                Console.WriteLine($"{moscowTime.Hour} не больше {endHour}");
                return false;
            }
            else
            {
                return true;
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Logger.Log(LogMessageType.Error, $"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
