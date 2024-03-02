# tgbot

appsettings.json 

{
  "BotSettings": [
    {
      "Name": "MainBot",
      "Token": "",
      "ChatId": "", // Канал
      "TempChatId": "" // Тестовый канал, который я использовал для отладки
    },
    {
      "Name": "BlizkiyBot",
      "Token": "",
      "ChatId": "", // Я не использую этот чатайди - бот автоматом пишет в канал, куда добавлен
      "TempChatId": ""
    }
  ],
  "FileLoggerOptions": {
    "FilePath": "E:\\tg\\log.txt"
  },
  "ImageProcessingOptions": {
    "FolderPath": "E:\\tg"  // Путь до картиночек, куда сейвить - и откуда потом постить
  }
}
