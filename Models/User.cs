using System;

namespace ProgrammingTutor.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "en"; // "en" or "sq"
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
