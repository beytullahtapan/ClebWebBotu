using ClebWebBot.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;

namespace ClebwebBot.commands
{
    public class StokKontrol : BaseCommandModule
    {
        private  readonly Context dbContext;
        private static BotSetting botSetting = new BotSetting();
        public StokKontrol(Context dbContext)
        {
            this.dbContext = dbContext;
        }

        [Command("Stok")]
        public async Task StokAded(CommandContext ctx)
        {
            using (var dbContext = new Context())
            {
                var botSetting = await dbContext.BotSettings.FirstOrDefaultAsync();
                if (botSetting == null)
                {
                    await ErrorLogger.LogErrorToDatabase($"Bot ayarları: ", "bot ayarlarına erişelemedi.");
                    return;
                }
                var newBotSetting = new BotSetting
                {
                    StockControlStatus = botSetting.StockControlStatus,
                };
                dbContext.BotSettings.Add(newBotSetting);
                dbContext.SaveChanges();
                if(newBotSetting.StockControlStatus == false)
                {
                    return;
                }
                var urlAndBarkodList = await GetUrlAndBarkodListAsync(dbContext);

                await SilOncekiMesajlari(ctx.Channel);
                string allResponses = "";
                foreach (var (id, url, barkodList) in urlAndBarkodList)
                {
                    string response = await GetStokAdediAsync(url, barkodList, id);
                    allResponses += response + "\n";
                }

                const int maxMessageLength = 2000;
                int currentIndex = 0;

                while (currentIndex < allResponses.Length)
                {
                    int length = Math.Min(maxMessageLength, allResponses.Length - currentIndex);
                    string chunk = allResponses.Substring(currentIndex, length);
                    await Task.Delay(2000);
                    await ctx.RespondAsync(chunk);
                    currentIndex += length;
                }
            }
        }

        private async Task<List<(int, string, List<string>)>> GetUrlAndBarkodListAsync(Context dbContext)
        {
            var data = await dbContext.Products.ToListAsync();
            return data.Where(item => item.UpdateStatus == true).Select(item => (item.ProductId, item.Url, item.Barcodes.Split(',').ToList())).ToList();
        }
        static async Task<string> GetStokAdediAsync(string url, List<string> barkodList,int productId)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var dbContext = new Context())
                {
                    var product = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

                    try
                    {
                        string html = await client.GetStringAsync(url);

                        int startIndex = html.IndexOf("var productDetailModel = ") + 25;
                        int endIndex = html.IndexOf(";", startIndex);

                        if (startIndex >= 25 && endIndex >= 0)
                        {
                            string productDetailJson = html.Substring(startIndex, endIndex - startIndex);
                            ProductDetail productDetail = JsonConvert.DeserializeObject<ProductDetail>(productDetailJson);

                            string response = "";

                            if (productDetail.ProductVariantData != null && productDetail.ProductVariantData.Count > 0)
                            {
                                response += $"Ürün : {productDetail.ProductName}\n";
                                response += $"Url : <{url}>\n";
                                List<int> newStockList = new List<int>();
                                string currentStockString = product.StockQuantity;
                                List<int> currentStockList = currentStockString.Split(',').Select(int.Parse).ToList();
                                foreach (var productVariant in productDetail.ProductVariantData)
                                {
                                    if (productVariant.Tanim != null && productVariant.tipid == 2)
                                    {
                                        var beden = productVariant.Tanim;
                                        var stokAdet = productVariant.stokAdedi;
                                        if (barkodList.Count > 0)
                                        {
                                            var barkod = barkodList[0];
                                            int stokAdetInt = Convert.ToInt32(stokAdet);
                                            newStockList.Add(stokAdetInt);
                                            response += $"Beden: {beden}, Stok Adet: {stokAdet}, Barkod: {barkod}\n";
                                            barkodList.RemoveAt(0);
                                        }
                                    }
                                }
                                return response;
                            }
                            else
                            {
                                await ErrorLogger.LogErrorToDatabase($"Stok Kontrol ProductVariantData: {url}", "VariantData boş.");
                                return "ProductVariantData boş..";
                            }
                        }
                        else
                        {
                            await ErrorLogger.LogErrorToDatabase($"Stok Kontrol productDetailModel: {url}", " Model Html de bulunamadı");
                            return "productDetailModel Html de bulunamadı.";
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        await ErrorLogger.LogErrorToDatabase($"Stok Kontrol GetStokAdediAsync URL: {url}", e.Message);
                        return $"Hata oluştu: {e.Message} - Link: {url}";
                    }
                }
            }
        }

        static async Task SilOncekiMesajlari(DiscordChannel channel)
        {
            var messages = await channel.GetMessagesAsync();
            foreach (var message in messages)
            {
                await message.DeleteAsync();
                await Task.Delay(2000);
            }
        }
    }
}
