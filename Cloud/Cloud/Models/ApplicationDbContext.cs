using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Cloud.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FileScheme> Files { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(@"Data Source=data.db");
    }
}
