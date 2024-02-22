using ConsoleApp;
using Core;
using Core.Logging;
using Infrastructure.Contracts;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Models.Options;

var serviceProvider = Startup.ConfigureServices();
var botOptions = serviceProvider.GetService<IOptions<BotOptions>>()?.Value;
var factory = new BotFactory(serviceProvider);
var logger = serviceProvider.GetService<ILogger>();

var tasks = new List<Task>();

try
{

    foreach (var botConfig in botOptions.BotSettings)
    {
        // Каждый botConfig содержит Name, BotToken, ChatId
        IBotService bot = factory.CreateBotService(botConfig);

        tasks.Add(bot.ExecuteAsync());
        tasks.Add(bot.StartRecieving());
    }

    await Task.WhenAll(tasks);
}
catch (Exception ex)
{
    logger.Log(LogMessageType.Info, $"Произошла ошибка: {ex.Message}");
}