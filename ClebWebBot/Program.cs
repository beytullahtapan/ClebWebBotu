using ClebwebBot.config;
using ClebWebBot.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace ClebwebBot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static System.Timers.Timer stokTimer;
        private static BotSetting botSetting = new BotSetting();

        static async Task Main(string[] args)
        {
            var jsonRead = new JsonRead();
            await jsonRead.ReadJson();

            var dbContext = new Context();
            
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonRead.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            Client.Ready += Client_Ready;

            var serviceProvider = new ServiceCollection()
                .AddSingleton(dbContext)
                .BuildServiceProvider();

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonRead.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
                Services = serviceProvider
            };
            var slahCommandsConfig = Client.UseSlashCommands();
            // Prefix Komutları 
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<commands.StokKontrol>();
            Commands.RegisterCommands<commands.StokGuncelle>();

            // Slash Komutları
            slahCommandsConfig.RegisterCommands<MessageSend>();

      
            StartStokTimer();
            StartTrednyolTimer();

            await Client.ConnectAsync();
            Client.ClientErrored += async (sender, args) =>
            {
                await ErrorLogger.LogErrorToDatabase("ClientError", args.Exception.Message);
            };
            await Task.Delay(-1);
        }

        private static void StartStokTimer()
        {
            using (var dbContext = new Context())
            {
                botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                var newBotSetting = new BotSetting
                {
                    StockControlTime = botSetting.StockControlTime,
                    StockControlStatus = botSetting.StockControlStatus,
                };
                dbContext.BotSettings.Add(newBotSetting);
                dbContext.SaveChanges();
                stokTimer = new System.Timers.Timer();
                stokTimer.Interval = newBotSetting.StockControlTime * 60 * 1000;
                stokTimer.AutoReset = true;
                stokTimer.Elapsed += async (sender, e) => await BellaNoteStok();
                stokTimer.Start();

              

            }
        }

        private static void StartTrednyolTimer()
        {
                using (var dbContext = new Context())
                {
                    botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                    var newBotSetting = new BotSetting
                    {
                        StockUpdateTime = botSetting.StockUpdateTime,
                    };
                    dbContext.BotSettings.Add(newBotSetting);
                    dbContext.SaveChanges();
                    stokTimer = new System.Timers.Timer();
                    stokTimer.Interval = newBotSetting.StockUpdateTime * 60 * 1000;
                    stokTimer.Elapsed += async (sender, e) => await TrendyolStok();
                    stokTimer.AutoReset = true;
                    stokTimer.Start();
                }

        }

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task BellaNoteStok()
        {
            try
            {
                using (var dbContext = new Context())
                {
                    botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                    var stokCommand = new commands.StokKontrol(dbContext);
                    var newBotSetting = new BotSetting
                    {
                        StokControlChannel = botSetting.StokControlChannel,
                        StockControlStatus = botSetting.StockControlStatus,
                    };
                    dbContext.BotSettings.Add(newBotSetting);
                    dbContext.SaveChanges();
                    ulong channelId = newBotSetting.StokControlChannel;

                    var channel = await Client.GetChannelAsync(channelId);
                    var fakeContext = Commands.CreateFakeContext(null, await Client.GetChannelAsync(channelId), "!", "Stok", null);
                    if(newBotSetting.StockControlStatus == false)
                    {
                        return;
                    }
                    await channel.SendMessageAsync("Stok Kontrol Ediliyor..");

                    await stokCommand.StokAded(fakeContext);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
            }
        }
        private static async Task TrendyolStok()
        {
            try
            {
                using (var dbContext = new Context())
                {
                    botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                    var stokCommand = new commands.StokGuncelle(dbContext);
                    var newBotSetting = new BotSetting
                    {
                        StockUpdateChannel = botSetting.StockUpdateChannel,
                    };
                    ulong channelId = newBotSetting.StockUpdateChannel;
                    dbContext.BotSettings.Add(newBotSetting);
                    dbContext.SaveChanges();
                    var channel = await Client.GetChannelAsync(channelId);

                    var fakeContext = Commands.CreateFakeContext(null, await Client.GetChannelAsync(channelId), "!", "Stokgüncelle", null);
                    await channel.SendMessageAsync("Stok Güncelleniyor..");

                    await stokCommand.StokAdedtrendyol(fakeContext);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
            }
        }

    }
}