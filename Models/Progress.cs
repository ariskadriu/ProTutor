using System.Collections.Generic;

namespace ProgrammingTutor.Models
{
    public class Progress
    {
        public List<string> CompletedLessons { get; set; } = new List<string>();
        public string ValidationKey { get; set; } = string.Empty;
    }

    public class UserSettings
    {
        public string Theme { get; set; } = "Dark";
    }

    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
    }
}
