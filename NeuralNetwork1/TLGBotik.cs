using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace NeuralNetwork1
{
    class TLGBotik
    {
        public TelegramBotClient client = null;
        private readonly AIMLService aiml;

        MagicEye proc = new MagicEye();

        //   GenerateImage generateImage = new GenerateImage();

        private UpdateTLGMessages formUpdater;

        private BaseNetwork perseptron = null;
        // CancellationToken - инструмент для отмены задач, запущенных в отдельном потоке
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        public string Username { get; }
        public TLGBotik(BaseNetwork net, UpdateTLGMessages updater)
        {
            aiml = new AIMLService();
            var botKey = System.IO.File.ReadAllText("botkey.txt");
            // generateImage.LoadImages();
            client = new TelegramBotClient(botKey);
            formUpdater = updater;
            perseptron = net;
        }

        public void SetNet(BaseNetwork net)
        {
            perseptron = net;
            formUpdater("Net updated!");
        }

        private async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //  Тут очень простое дело - банально отправляем назад сообщения
            var message = update.Message;
            var chatId = message.Chat.Id;
            var username = message.Chat.FirstName;
            formUpdater("Тип сообщения : " + message.Type.ToString());

            //  Получение файла (картинки)
            if (message.Type == MessageType.Photo)
            {
                formUpdater("Picture loadining started");
                var photoId = message.Photo.Last().FileId;
                Telegram.Bot.Types.File fl = client.GetFileAsync(photoId).Result;
                var imageStream = new MemoryStream();
                await client.DownloadFileAsync(fl.FilePath, imageStream, cancellationToken: cancellationToken);
                var img = Image.FromStream(imageStream);

                Bitmap bm = new Bitmap(img);

                //  Масштабируем aforge
                // AForge.Imaging.Filters.ResizeBilinear scaleFilter = new AForge.Imaging.Filters.ResizeBilinear(200,200);
                // var uProcessed = scaleFilter.Apply(AForge.Imaging.UnmanagedImage.FromManagedImage(bm));

                proc.ProcessImage(bm);
                var procImage = proc.processed;
                var sample = getBmp(procImage);

                switch (perseptron.Predict(sample))
                {
                    case FigureType.canBleached: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был canBleached!"); break;
                    case FigureType.drying: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был drying!"); break;
                    case FigureType.iron: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был iron"); break;
                    case FigureType.canTBleached: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был canTBleached!"); break;
                    case FigureType.dontwash: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был dontwash!"); break;
                    case FigureType.spinIsProhibited: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был spinIsProhibited!"); break;
                    case FigureType.machineWashable: client.SendTextMessageAsync(message.Chat.Id, "Думаю, это был machineWashable!"); break;
                    default: client.SendTextMessageAsync(message.Chat.Id, "Я такого не знаю!"); break;
                }

                formUpdater("Picture recognized!");
                return;
            }
            else if (message.Type == MessageType.Text)
            {
                var messageText = update.Message.Text;

                Console.WriteLine($"Received a '{messageText}' message in chat {chatId} with {username}.");

                // Echo received message text
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: aiml.Talk(chatId, username, messageText),
                    cancellationToken: cancellationToken);
                return;
            }

            if (message.Type == MessageType.Video)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Видео"), cancellationToken: cancellationToken);
                return;
            }
            if (message.Type == MessageType.Audio)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Аудио"), cancellationToken: cancellationToken);
                return;
            }
        }

        private Sample getBmp(Bitmap img)
        {
            double[] input = new double[400];
            for (int i = 0; i < 400; i++)
                input[i] = 0;

            Color prev = img.GetPixel(0, 0);

            for (int i = 0; i < 200; i++)
                for (int j = 0; j < 200; j++)
                {
                    if (img.GetPixel(i, j).R != 255 && img.GetPixel(i, j).G != 255 && img.GetPixel(i, j).B != 255)
                    {
                        if (prev.R == 255 && prev.G == 255 && prev.B == 255)
                        {
                            prev = img.GetPixel(i, j);
                            input[i] += 1;
                            input[200 + j] += 1;
                        }
                    }
                    else if (img.GetPixel(i, j).R == 255 && img.GetPixel(i, j).G == 255 && img.GetPixel(i, j).B == 255)
                    {
                        if (prev.R != 255 && prev.G != 255 && prev.B != 255)
                        {
                            prev = img.GetPixel(i, j);
                            input[i] += 1;
                            input[200 + j] += 1;
                        }
                    }

                }
            Sample sample = new Sample(input, 7);
            return sample;
        }
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var apiRequestException = exception as ApiRequestException;
            if (apiRequestException != null)
                Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
            else
                Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }

        public bool Act()
        {
            try
            {
                client.StartReceiving(HandleUpdateMessageAsync, HandleErrorAsync, new ReceiverOptions
                {   // Подписываемся только на сообщения
                    AllowedUpdates = new[] { UpdateType.Message }
                },
                cancellationToken: cts.Token);
                // Пробуем получить логин бота - тестируем соединение и токен
                Console.WriteLine($"Connected as {client.GetMeAsync().Result}");
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public void Stop()
        {
            cts.Cancel();
        }

    }
}
