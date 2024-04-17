using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClebWebBot.Models
{
    public class LogEntry
    {
        [Key]
        public int Id { get; set; }
        public string Log { get; set; }
        public DateTime Date { get; set; }
        public string DSharpPlusVersion { get; set; }
    }
}
