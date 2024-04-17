using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClebWebBot.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Url { get; set; }
        public string Barcodes { get; set; }
        public string StockQuantity { get; set; }
        public bool UpdateStatus { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
