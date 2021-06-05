using Microsoft.EntityFrameworkCore;

namespace Cloud.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<FileShare> Shares { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite(@"Data Source=data.db");
        }
    }
}