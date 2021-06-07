using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud.Models
{
    public class DbFile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Filename { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }
}
