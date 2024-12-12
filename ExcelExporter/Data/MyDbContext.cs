using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelExporter.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelExporter.Data
{
    public class MyDbContext : DbContext
    {
        public DbSet<Model> Models { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=FactoryNumberList.db");
        }
    }
}
