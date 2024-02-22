using Core.Logging;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Infrastructure.Services;
using Infrastructure.Contracts;

namespace Core.Services
{
    public class BlizkiyBotService : BotServiceBase
    {
        private readonly IImageService _imageService;

        public BlizkiyBotService(IImageService imageService, ILogger logger) : base(logger)
        {
            _imageService = imageService;
        }

        public override async Task ExecuteAsync()
        {
            // var logger = this.Logger;
            User me = await BotClient.GetMeAsync();
            Logger.Log(LogMessageType.Info, "BlizkiyBot начал выполнение");

            CancellationTokenSource cts = new CancellationTokenSource();
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Получать все типы обновлений
            };
            BotClient.StartReceiving(
                async (bc, update, ct) => await HandleUpdateAsync(bc, update, ct),
                async (bc, exception, ct) =>
                {
                    Logger.Log(LogMessageType.Error, $"Ошибка BlizkiyBot: {exception.Message}");
                    // Если ошибка - подождём пару секунд, чтобы не срать ошибками в чат
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                    // return Task.CompletedTask;
                },
                receiverOptions,
                cts.Token
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                string imagePath = _imageService.GetImagePath(update.Message.Text);
                if (imagePath != null)
                {
                    using (FileStream stream = System.IO.File.OpenRead(imagePath))
                    {
                        _ = await botClient.SendAnimationAsync(
                            chatId: update.Message.Chat.Id,
                            animation: InputFile.FromStream(stream, Path.GetFileName(imagePath)),
                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
        }
        public override async Task StartRecieving()
        {

        }
    }
}
