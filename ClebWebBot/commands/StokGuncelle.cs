using ClebWebBot.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ClebwebBot.commands
{
    public class StokGuncelle : BaseCommandModule
    {
        private readonly Context dbContext;
        public StokGuncelle(Context dbContext)
        {
            this.dbContext = dbContext;
        }

        [Command("Stokgüncelle")]
        public async Task StokAdedtrendyol(CommandContext ctx)
        {
            using (var dbContext = new Context())
            {
                var urlAndBarkodList = await GetUrlAndBarkodListAsync(dbContext);
                await SilOncekiMesajlari(ctx.Channel);
                string allResponses = "";
                foreach (var (id, url, barkodList, stokaded) in urlAndBarkodList)
                {
                    string response = await GetStokAdediAsync(url, barkodList, stokaded, ctx, id);
                    if (response != null)
                    {
                        allResponses += response;
                    }
                }

                const int maxMessageLength = 2000;
                int currentIndex = 0;
                while (currentIndex < allResponses.Length)
                {
                    int length = Math.Min(maxMessageLength, allResponses.Length - currentIndex);
                    string chunk = allResponses.Substring(currentIndex, length);
                    await ctx.RespondAsync(chunk);
                    currentIndex += length;
                }
            }
        }

        private async Task<List<(int, string, List<string>, List<string>)>> GetUrlAndBarkodListAsync(Context dbContext)
        {
            var data = await dbContext.Products.ToListAsync();
            return data.Where(item => item.UpdateStatus == true)
                       .Select(item => (
                           item.ProductId,
                           item.Url,
                           item.Barcodes.Split(',').ToList(),
                           item.StockQuantity.Split(',').ToList()
                       )).ToList();

        }


        private async Task<string> GetStokAdediAsync(string url, List<string> barkodList, List<string> stokaded, CommandContext ctx, int productId)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var dbContext = new Context())
                {
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
                                var product = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                                bool isStockUpdated = false;
                                string stoks = "";
                                var botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                                foreach (var productVariant in productDetail.ProductVariantData)
                                {
                                    if (productVariant.Tanim != null && productVariant.tipid == 2)
                                    {
                                        var stokAdet = productVariant.stokAdedi;
                                        if (barkodList.Count > 0)
                                        {
                                            var barkod = barkodList[0];
                                            var stokadet = stokaded[0];
                                            int stokAdetInt = Convert.ToInt32(stokAdet);
                                            int veristok = Convert.ToInt32(stokadet);
                                            if (botSetting.StockCountStatus && stokAdetInt <= botSetting.StockCount)
                                            {
                                                stoks += "0,";
                                                stokAdetInt = 0;
                                                isStockUpdated = true;
                                            }
                                            else
                                            {
                                                stoks += stokAdet + ",";
                                                isStockUpdated = true;
                                            }
                                            if (stokAdetInt != veristok && stokAdetInt != 0)
                                            {
                                                response += await UpdateStokAsync(ctx, barkod, stokAdetInt);
                                                response += "\n";
                                                isStockUpdated = true;
                                            }
                                            barkodList.RemoveAt(0);
                                            stokaded.RemoveAt(0);
                                        }
                                    }
                                }
                                if (product != null && isStockUpdated)
                                {
                                    product.LastUpdate = DateTime.UtcNow;
                                    product.StockQuantity = stoks.TrimEnd(',');
                                    dbContext.Update(product);
                                    await dbContext.SaveChangesAsync();
                                }
                                return response;
                            }
                            else
                            {
                                await ErrorLogger.LogErrorToDatabase($"Stok Güncelle ProductVariantData: {url}", "VariantData boş.");
                                return "ProductVariantData boş..";
                            }
                        }
                        else
                        {
                            await ErrorLogger.LogErrorToDatabase($"Stok Güncelle productDetailModel: {url}", " Model Html de bulunamadı");
                            return "productDetailModel Html de bulunamadı.";
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        await ErrorLogger.LogErrorToDatabase($"Stok Güncelle GetStokAdediAsync URL: {url}", e.Message);
                        return $"Hata oluştu: {e.Message} - Link: {url}";
                    }
                }
            }
        }

        private static async Task SilOncekiMesajlari(DiscordChannel channel)
        {
            var messages = await channel.GetMessagesAsync();
            foreach (var message in messages)
            {
                await message.DeleteAsync();
                await Task.Delay(2000);
            }
        }

        private async Task<string> UpdateStokAsync(CommandContext ctx, string barkod, int stokAdedi)
        {

            using (var httpClient = new HttpClient())
            {
                using (var dbContext = new Context())
                {
                    var botSetting = dbContext.BotSettings.FirstOrDefault(b => b.Id == 1);
                    if (botSetting != null)
                    {
                        string userAgent = $"{botSetting.SupplerId} - SelfIntegration";
                        string apiUrl = $"https://api.trendyol.com/sapigw/suppliers/{botSetting.SupplerId}/products/price-and-inventory";
                        string authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{botSetting.apiKey}:{botSetting.apiSecret}"));
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
                        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
                        var content = new StringContent($"{{\"items\": [{{\"barcode\": \"{barkod}\", \"quantity\": {stokAdedi}}}]}}",
                                                       Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(apiUrl, content);

                        string responseString;
                        if (response.IsSuccessStatusCode)
                        {
                            return responseString = $"Stok güncellendi:{barkod}:{stokAdedi}";
                        }
                        else
                        {
                            await ErrorLogger.LogErrorToDatabase($"Stok güncellenemedi | barkod: {barkod}", response.ReasonPhrase);
                            return responseString = $"Stok güncellenemedi: {barkod}:{stokAdedi}";
                        }
                    }
                    else
                    {
                        await ErrorLogger.LogErrorToDatabase($"Bot Settings Hatası:", "BotSettings boş.");
                        return "BotSetting değeri bulunamadı.";
                    }
                }
            }
        }

    }
}
