using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClebWebBot.Models
{
    public class ErrorLogger
    {
        public static async Task LogErrorToDatabase(string methodName, string errorMessage)
        {
            try
            {
                var dbContext = new Context();
                var logEntry = new LogEntry
                {
                    Log = $"Method: {methodName}, Error: {errorMessage}",
                    Date = DateTime.Now,
                    DSharpPlusVersion = typeof(DiscordClient).Assembly.GetName().Version.ToString()
                };

                dbContext.LogEntries.Add(logEntry);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Hata oluştu: {logEx.Message}");
            }
        }
    }
}
