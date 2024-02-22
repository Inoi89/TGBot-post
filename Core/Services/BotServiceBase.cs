using Core.Logging;
using Infrastructure.Contracts;
using Infrastructure.Services;
using Models.Options;
using Telegram.Bot;

namespace Core.Services
{
    public abstract class BotServiceBase : IBotService
    {
        protected readonly ILogger Logger;

        protected BotServiceBase(ILogger logger)
        {
            Logger = logger;
        }

        protected TelegramBotClient BotClient { get; private set; }
        protected BotSettings BotSettings { get; private set; }

        public void Configure(BotSettings botSettings)
        {
            BotSettings = botSettings;
            BotClient = new TelegramBotClient(BotSettings.Token);
        }

        public abstract Task ExecuteAsync();
        public abstract Task StartRecieving();
    }
}
