using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClebWebBot.Models
{
    public class Context : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("*");
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<BotSetting> BotSettings { get; set; }

        
    }

}
