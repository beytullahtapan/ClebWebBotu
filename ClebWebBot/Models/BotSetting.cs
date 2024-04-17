using System.ComponentModel.DataAnnotations;

namespace ClebWebBot.Models
{
    public class BotSetting
    {
        [Key]
        public int Id { get; set; }
        public bool BotStatus { get; set; }

        public bool StockControlStatus { get; set; }
        public int StockControlTime { get; set; }
        public ulong StokControlChannel { get; set; }


        public int StockUpdateTime { get; set; }
        public ulong StockUpdateChannel { get; set; }


        public string? SupplerId { get; set; }

        public string? apiKey { get; set; }
        public string? apiSecret { get; set; }

        public int StockCount { get; set; }
        public bool StockCountStatus { get; set; }

    }
}
