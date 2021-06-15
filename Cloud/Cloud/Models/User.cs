using System;

namespace Cloud.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string FilePassword { get; set; }
        public bool IsAdmin { get; set; }
        public long MaxFileBytes { get; set; }
        public DateTime LastLogin { get; set; }
        public bool ShowKnownFileExtensions { get; set; } = true;
        public string TotpSecret { get; set; }
        public bool TotpActive { get; set; }
    }
}