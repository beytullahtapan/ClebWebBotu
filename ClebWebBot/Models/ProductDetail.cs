using ClebwebBot.commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClebWebBot.Models
{
    public class ProductDetail
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string stockCode { get; set; }
        public string ProductShortDescription { get; set; }
        public string ProductUrl { get; set; }
        public bool ProductIsAsorti { get; set; }
        public List<ProductVariant> ProductVariantData { get; set; }
    }
}
