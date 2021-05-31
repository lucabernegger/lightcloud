using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud.Models
{
    public class FileScheme
    {
        public int Id { get; set; }
        public string FileIdentifier { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string FileHash { get; set; }
        public string Size { get; set; }
        public DateTime Modified { get; set; }
    }
}
