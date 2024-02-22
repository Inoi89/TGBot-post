using Core.Services;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Models.Options;

namespace Core
{
    public class BotFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public BotFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBotService CreateBotService(BotSettings botSettings)
        {
            IBotService result;
            switch (botSettings.Name)
            {
                case "BlizkiyBot":
                    result = _serviceProvider.GetService<BlizkiyBotService>();
                    result?.Configure(botSettings);
                    return result;
                case "MainBot":
                    result = _serviceProvider.GetService<DavaySimBotService>();
                    result?.Configure(botSettings);
                    return result;
                // Другие кейсы для других ботов
                default:
                    throw new ArgumentException("Нет такого бота", nameof(botSettings.Name));
            }
        }
    }
}
